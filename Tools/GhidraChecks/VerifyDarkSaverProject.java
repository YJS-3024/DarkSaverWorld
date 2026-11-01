// Reopen and verify the persisted analysis project without modifying it.
// @category DarkSaver
import ghidra.app.script.GhidraScript;
import ghidra.program.model.data.*;
import ghidra.program.model.listing.*;
import com.google.gson.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;

public class VerifyDarkSaverProject extends GhidraScript {
    @Override public void run() throws Exception {
        String[] args = getScriptArgs();
        JsonObject plan = JsonParser.parseString(Files.readString(Path.of(args[0]))).getAsJsonObject();
        JsonObject selected = null;
        for (JsonElement e : plan.getAsJsonArray("targets")) {
            JsonObject t = e.getAsJsonObject();
            if (Path.of(t.get("path").getAsString()).getFileName().toString().equalsIgnoreCase(currentProgram.getName())) selected = t;
        }
        if (selected == null) throw new IllegalStateException("Unplanned project program");
        String hash = HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(
            Files.readAllBytes(Path.of(selected.get("path").getAsString()))));
        if (!hash.equals(selected.get("sha256").getAsString())) throw new IllegalStateException("Source changed");
        int count = 0;
        for (JsonElement e : selected.getAsJsonArray("functions")) {
            JsonObject item = e.getAsJsonObject();
            Function function = getFunctionAt(toAddr(item.get("address").getAsString()));
            if (function == null || !function.getName().equals(item.get("name").getAsString()))
                throw new IllegalStateException("Saved function annotation missing: " + item.get("address"));
            byte[] code = new byte[32]; currentProgram.getMemory().getBytes(function.getEntryPoint(), code);
            if (!HexFormat.of().formatHex(code).equals(item.get("entryBytes32").getAsString()))
                throw new IllegalStateException("Saved instruction prefix changed");
            if (item.has("requiredCallSite") && !function.getBody().contains(toAddr(item.get("requiredCallSite").getAsString())))
                throw new IllegalStateException("Required native call not in saved body");
            count++;
        }
        DataTypeManager manager = currentProgram.getDataTypeManager();
        Structure unit = (Structure)manager.getDataType("/DarkSaverRecovered/NativeUnitInfo32");
        Structure character = (Structure)manager.getDataType("/DarkSaverRecovered/NativeCharacter32");
        if (unit == null || unit.getLength() != 0xb0 || character == null || character.getLength() != 0x2e0)
            throw new IllegalStateException("Partial native structure sizes changed");
        if (!unit.getComponentAt(0x48).getFieldName().equals("tileX") ||
            !unit.getComponentAt(0x4c).getFieldName().equals("tileY") ||
            !character.getComponentAt(0x70).getFieldName().equals("tileX") ||
            !character.getComponentAt(0x74).getFieldName().equals("tileY"))
            throw new IllegalStateException("Saved coordinate fields changed");
        if (selected.get("name").getAsString().equals("UserUnit")) {
            Function apply = getFunctionAt(toAddr("0x1000b0a0"));
            if (!apply.getParameter(0).getDataType().getName().contains("NativeUnitInfo32"))
                throw new IllegalStateException("Snapshot parameter type did not persist");
            Data global = getDataAt(toAddr("0x1010b3a0"));
            if (global == null || !global.getDataType().getName().contains("NativeCharacter32"))
                throw new IllegalStateException("Runtime character pointer type did not persist");
        }
        JsonObject result = new JsonObject(); result.addProperty("target", selected.get("name").getAsString());
        result.addProperty("functionsVerified", count); result.addProperty("entryBytesPerFunction", 32);
        result.addProperty("sourceSha256", hash); result.addProperty("sourceUnchanged", true);
        result.addProperty("reopenedProject", true); result.addProperty("typesVerified", true);
        Path output = Path.of(args[1]); Files.createDirectories(output);
        Files.writeString(output.resolve(selected.get("name").getAsString() + ".json"),
            new GsonBuilder().setPrettyPrinting().create().toJson(result) + "\n");
        println("DARKSAVER_VERIFIED " + result);
    }
}
