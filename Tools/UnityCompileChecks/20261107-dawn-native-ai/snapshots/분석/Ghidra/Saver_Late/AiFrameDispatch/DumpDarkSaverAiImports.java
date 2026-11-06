// @category DarkSaver
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import java.nio.file.*;
import java.util.*;
public class DumpDarkSaverAiImports extends GhidraScript {
 public void run()throws Exception {
  Path out=Path.of(getScriptArgs()[0]);Files.createDirectories(out);StringBuilder log=new StringBuilder();
  for(var file:currentProgram.getDomainFile().getParent().getFiles())println("DOMAINFILE "+file.getName());
  log.append(currentProgram.getName()+" "+currentProgram.getExecutableSHA256()+"\n");Set<Function> found=new LinkedHashSet<>();
  var symbols=currentProgram.getSymbolTable().getAllSymbols(true);
  while(symbols.hasNext()){
   var symbol=symbols.next();String name=symbol.getName().toLowerCase();
   if(!(name.equals("update")||name.startsWith("ptr_update_")||name.contains("aiexcute")||name.contains("aistart")||name.contains("aiend")||name.contains("aiflag")||name.contains("checkai")||name.contains("setmonsterai")))continue;
   log.append("SYMBOL "+symbol.getAddress()+" "+symbol.getName()+"\n");
   Function target=getFunctionAt(symbol.getAddress());if(target!=null)found.add(target);
   for(var ref:getReferencesTo(symbol.getAddress())){var f=getFunctionContaining(ref.getFromAddress());log.append("  "+ref+" owner="+(f==null?"none":f.getEntryPoint()+" "+f.getName())+"\n");if(f!=null)found.add(f);}
  }
  Set<Function> level=new LinkedHashSet<>(found);
  for(int depth=0;depth<4;depth++){
   Set<Function> next=new LinkedHashSet<>();
   for(var f:level)for(var caller:f.getCallingFunctions(monitor))if(found.add(caller)){next.add(caller);log.append("CALLER "+caller.getEntryPoint()+" "+caller.getName()+" -> "+f.getEntryPoint()+" "+f.getName()+"\n");}
   level=next;if(level.isEmpty())break;
  }
  var dc=new DecompInterface();dc.openProgram(currentProgram);
  for(var f:found){if(f.isExternal())continue;var d=dc.decompileFunction(f,45,monitor);Files.writeString(out.resolve(f.getEntryPoint()+".c"),d.decompileCompleted()?d.getDecompiledFunction().getC():d.getErrorMessage());}
  dc.dispose();Files.writeString(out.resolve("references.txt"),log.toString());println(log.toString());
 }
}
