// Apply evidence-backed names and partial layouts to a separate Ghidra project.
// Original PE files are never patched or exported as executables.
// @category DarkSaver
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.program.model.address.Address;
import ghidra.program.model.data.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.listing.Function.FunctionUpdateType;
import ghidra.program.model.symbol.SourceType;
import com.google.gson.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;

public class ApplyDarkSaverRefactor extends GhidraScript {
    private final Gson gson = new GsonBuilder().setPrettyPrinting().create();
    private DataType unitInfo;
    private DataType character;

    private void field(StructureDataType structure, int offset, String name) {
        structure.replaceAtOffset(offset, IntegerDataType.dataType, 4, name,
            "Observed native32 offset; surrounding undefined bytes remain unresolved.");
    }

    private void defineLayouts() throws Exception {
        DataTypeManager manager = currentProgram.getDataTypeManager();
        CategoryPath category = new CategoryPath("/DarkSaverRecovered");
        StructureDataType unit = new StructureDataType(category, "NativeUnitInfo32", 0xb0);
        field(unit, 0x00, "unitId"); field(unit, 0x04, "jobId");
        field(unit, 0x48, "tileX"); field(unit, 0x4c, "tileY");
        unitInfo = manager.addDataType(unit, DataTypeConflictHandler.REPLACE_HANDLER);
        StructureDataType live = new StructureDataType(category, "NativeCharacter32", 0x2e0);
        field(live, 0x00, "active"); field(live, 0x10, "unitId");
        field(live, 0x18, "jobId"); field(live, 0x70, "tileX");
        field(live, 0x74, "tileY"); field(live, 0x108, "aiState");
        character = manager.addDataType(live, DataTypeConflictHandler.REPLACE_HANDLER);
    }

    private void snapshotParameter(Function function) throws Exception {
        function.setCallingConvention("__cdecl");
        function.setReturnType(IntegerDataType.dataType, SourceType.USER_DEFINED);
        Parameter parameter = new ParameterImpl("snapshot", new PointerDataType(unitInfo, 4), currentProgram);
        function.replaceParameters(FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS, true,
            SourceType.USER_DEFINED, parameter);
    }

    @Override public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) throw new IllegalArgumentException("plan.json output-directory required");
        JsonObject plan = JsonParser.parseString(Files.readString(Path.of(args[0]))).getAsJsonObject();
        JsonObject selected = null;
        for (JsonElement element : plan.getAsJsonArray("targets")) {
            JsonObject candidate = element.getAsJsonObject();
            if (Path.of(candidate.get("path").getAsString()).getFileName().toString()
                .equalsIgnoreCase(currentProgram.getName())) selected = candidate;
        }
        if (selected == null) throw new IllegalArgumentException("No reviewed plan for " + currentProgram.getName());
        byte[] source = Files.readAllBytes(Path.of(selected.get("path").getAsString()));
        String hash = HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(source));
        if (!hash.equals(selected.get("sha256").getAsString())) throw new IllegalStateException("Original identity mismatch");
        defineLayouts();
        List<String> renamed = new ArrayList<>();
        List<String> recovered = new ArrayList<>();
        for (JsonElement element : selected.getAsJsonArray("functions")) {
            JsonObject item = element.getAsJsonObject();
            Address entry = toAddr(item.get("address").getAsString());
            if (item.has("entryBytes32")) {
                byte[] actual = new byte[32]; currentProgram.getMemory().getBytes(entry, actual);
                if (!HexFormat.of().formatHex(actual).equals(item.get("entryBytes32").getAsString()))
                    throw new IllegalStateException("Imported code bytes differ at " + entry);
            }
            Function function = getFunctionAt(entry);
            if (function == null && item.has("recoverBoundary") && item.get("recoverBoundary").getAsBoolean()) {
                disassemble(entry);
                function = createFunction(entry, item.get("name").getAsString());
                recovered.add(item.get("address").getAsString());
            }
            if (function == null) throw new IllegalStateException("Missing exact function entry " + item.get("address"));
            if (item.has("requiredCallSite") && !function.getBody().contains(toAddr(item.get("requiredCallSite").getAsString())))
                throw new IllegalStateException("Recovered boundary does not include verified call site " + item.get("requiredCallSite"));
            function.setName(item.get("name").getAsString(), SourceType.USER_DEFINED);
            function.setComment(item.get("comment").getAsString() + "\nSource SHA256: " + hash);
            if (function.getName().equals("ApplyUnitSnapshot")) snapshotParameter(function);
            renamed.add(function.getName());
        }
        if (selected.get("name").getAsString().equals("UserUnit")) {
            Address global = toAddr("0x1010b3a0");
            DataUtilities.createData(currentProgram, global, new PointerDataType(character, 4), -1,
                false, DataUtilities.ClearDataMode.CLEAR_ALL_CONFLICT_DATA);
            createLabel(global, "g_RuntimeCharacters", true);
        }
        if (selected.get("name").getAsString().equals("GameCtl")) {
            FunctionIterator external = currentProgram.getFunctionManager().getExternalFunctions();
            while (external.hasNext()) {
                Function function = external.next();
                if (function.getName().contains("resetcharacter")) snapshotParameter(function);
            }
        }
        Path output = Path.of(args[1], selected.get("name").getAsString());
        Files.createDirectories(output);
        DecompInterface decompiler = new DecompInterface();
        if (!decompiler.openProgram(currentProgram)) throw new IllegalStateException("Decompiler could not open program");
        try {
            for (JsonElement element : selected.getAsJsonArray("functions")) {
                JsonObject item = element.getAsJsonObject();
                Function function = getFunctionAt(toAddr(item.get("address").getAsString()));
                DecompileResults result = decompiler.decompileFunction(function, 60, monitor);
                if (!result.decompileCompleted()) throw new IllegalStateException(result.getErrorMessage());
                String code = "/* Typed Ghidra reconstruction; not original source or an executable patch.\n" +
                    " * Entry " + item.get("address").getAsString() + "; source SHA256 " + hash + "\n */\n" +
                    result.getDecompiledFunction().getC();
                Files.writeString(output.resolve(function.getName() + ".c"), code, StandardCharsets.UTF_8);
            }
        } finally { decompiler.dispose(); }
        JsonObject report = new JsonObject(); report.addProperty("target", currentProgram.getName());
        report.addProperty("sha256", hash); report.addProperty("annotatedFunctions", renamed.size());
        report.add("names", gson.toJsonTree(renamed)); report.addProperty("unitInfoSize", unitInfo.getLength());
        report.add("recoveredFunctionEntries", gson.toJsonTree(recovered));
        report.addProperty("characterStride", character.getLength());
        report.addProperty("scope", "Separate persistent Ghidra project metadata/types; no original PE patch or execution");
        Files.writeString(output.resolve("typed-project-report.json"), gson.toJson(report) + "\n");
        println("DARKSAVER_REFACTOR " + gson.toJson(report));
    }
}
