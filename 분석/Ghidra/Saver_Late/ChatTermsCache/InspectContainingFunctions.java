import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import java.nio.file.*;
import java.util.*;
public class InspectContainingFunctions extends GhidraScript{public void run()throws Exception{String[] a=getScriptArgs();Path out=Path.of(a[0]);Files.createDirectories(out);DecompInterface d=new DecompInterface();d.openProgram(currentProgram);Set<String> seen=new HashSet<>();for(int i=1;i<a.length;i++){var f=getFunctionContaining(toAddr(a[i]));if(f==null)throw new Exception("No actual function at "+a[i]);String key=f.getEntryPoint().toString();if(!seen.add(key))continue;var r=d.decompileFunction(f,60,monitor);if(!r.decompileCompleted())throw new Exception(r.getErrorMessage());Files.writeString(out.resolve(key+".c"),r.getDecompiledFunction().getC());println(a[i]+" -> "+key);}d.dispose();}}
