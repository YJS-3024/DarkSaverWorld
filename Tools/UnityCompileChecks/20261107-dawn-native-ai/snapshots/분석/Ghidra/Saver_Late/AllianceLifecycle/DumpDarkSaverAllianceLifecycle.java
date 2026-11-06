// @category DarkSaver
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import java.nio.file.*;
import java.util.*;
public class DumpDarkSaverAllianceLifecycle extends GhidraScript {
 public void run() throws Exception {
  Path out=Path.of(getScriptArgs()[0]);Files.createDirectories(out);StringBuilder log=new StringBuilder(currentProgram.getName()+" "+currentProgram.getExecutableSHA256()+"\n");Set<Function> found=new LinkedHashSet<>();
  for(long offset=0x1003d540L;offset<0x1003d5a0L;offset++)for(var ref:getReferencesTo(toAddr(offset))){Function f=getFunctionContaining(ref.getFromAddress());log.append("GLOBAL "+Long.toHexString(offset)+" "+ref+" owner="+(f==null?"none":f.getEntryPoint()+" "+f.getName())+"\n");if(f!=null)found.add(f);}
  var symbols=currentProgram.getSymbolTable().getAllSymbols(true);while(symbols.hasNext()){var symbol=symbols.next();if(!symbol.getName().toLowerCase().contains("ally"))continue;log.append("SYMBOL "+symbol.getAddress()+" "+symbol.getName()+"\n");Function f=getFunctionAt(symbol.getAddress());if(f!=null)found.add(f);}
  Set<Function> level=new LinkedHashSet<>(found);for(int depth=0;depth<3;depth++){Set<Function> next=new LinkedHashSet<>();for(Function f:level)for(Function caller:f.getCallingFunctions(monitor))if(found.add(caller)){next.add(caller);log.append("CALLER "+caller.getEntryPoint()+" "+caller.getName()+" -> "+f.getEntryPoint()+"\n");}level=next;if(level.isEmpty())break;}
  DecompInterface dc=new DecompInterface();dc.openProgram(currentProgram);for(Function f:found){if(f.isExternal())continue;var d=dc.decompileFunction(f,45,monitor);Files.writeString(out.resolve(f.getEntryPoint()+".c"),d.decompileCompleted()?d.getDecompiledFunction().getC():d.getErrorMessage());StringBuilder asm=new StringBuilder();for(var ins:currentProgram.getListing().getInstructions(f.getBody(),true)){asm.append(ins.getAddress()+" "+ins+"\n");}Files.writeString(out.resolve(f.getEntryPoint()+".asm.txt"),asm.toString());}dc.dispose();Files.writeString(out.resolve("references.txt"),log.toString());println(log.toString());
 }
}
