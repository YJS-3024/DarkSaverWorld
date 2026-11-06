import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.address.*;
import java.nio.file.*;
import java.util.*;
public class DumpDarkSaverRange extends GhidraScript {
 public void run()throws Exception {
  String[] a=getScriptArgs();Path out=Path.of(a[0]);Files.createDirectories(out);var dc=new DecompInterface();dc.openProgram(currentProgram);Set<Function> found=new LinkedHashSet<>();StringBuilder log=new StringBuilder();
  log.append(currentProgram.getExecutableSHA256()+"\n");
  for(int i=1;i<a.length;i+=2){AddressSet range=new AddressSet(toAddr(a[i]),toAddr(a[i+1]));log.append("RANGE "+a[i]+".."+a[i+1]+"\n");var iterator=currentProgram.getReferenceManager().getReferenceDestinationIterator(range,true);
   while(iterator.hasNext()){var destination=iterator.next();for(var r:getReferencesTo(destination)){var f=getFunctionContaining(r.getFromAddress());log.append(destination+" <- "+r.getFromAddress()+" "+r.getReferenceType()+" "+(f==null?"none":f.getEntryPoint()+" "+f.getName())+"\n");if(f!=null)found.add(f);}}}
  Files.writeString(out.resolve("references.txt"),log.toString());
  for(var f:found){var d=dc.decompileFunction(f,45,monitor);Files.writeString(out.resolve(f.getEntryPoint()+".c"),d.decompileCompleted()?d.getDecompiledFunction().getC():d.getErrorMessage());}dc.dispose();println(log.toString());
 }
}
