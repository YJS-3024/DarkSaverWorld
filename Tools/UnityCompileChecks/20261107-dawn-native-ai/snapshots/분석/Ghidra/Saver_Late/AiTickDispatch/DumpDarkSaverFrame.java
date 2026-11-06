// @category DarkSaver
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.nio.file.*;
public class DumpDarkSaverFrame extends GhidraScript {
    public void run() throws Exception {
        String[] args=getScriptArgs();Path out=Path.of(args[0]);Files.createDirectories(out);
        println("PROGRAM_SHA256 "+currentProgram.getExecutableSHA256());
        DecompInterface decompiler=new DecompInterface();decompiler.openProgram(currentProgram);
        for(int i=1;i<args.length;i++) {
            var address=toAddr(args[i]);Function function=getFunctionContaining(address);
            if(function==null){disassemble(address);function=createFunction(address,null);}
            if(function==null){println("MISSING "+args[i]);continue;}
            var result=decompiler.decompileFunction(function,45,monitor);
            String text=result.decompileCompleted()?result.getDecompiledFunction().getC():result.getErrorMessage();
            StringBuilder refs=new StringBuilder();
            for(var ref:getReferencesTo(function.getEntryPoint()))refs.append(ref.getFromAddress()+" "+ref.getReferenceType()+"\n");
            for(var caller:function.getCallingFunctions(monitor)) {
                refs.append("CALLER "+caller.getEntryPoint()+" "+caller.getName()+"\n");
                for(var ref:getReferencesTo(caller.getEntryPoint()))refs.append("  "+ref.getFromAddress()+" "+ref.getReferenceType()+"\n");
            }
            Files.writeString(out.resolve(args[i]+".c"),text+"\n/* incoming refs\n"+refs+"*/\n");
            println("DUMPED "+function.getEntryPoint()+" "+function.getName());
        }
        decompiler.dispose();
    }
}
