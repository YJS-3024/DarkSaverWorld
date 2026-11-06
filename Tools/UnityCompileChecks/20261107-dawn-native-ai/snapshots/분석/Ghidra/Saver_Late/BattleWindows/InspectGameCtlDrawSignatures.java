import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;
import ghidra.program.model.symbol.SourceType;
import java.nio.file.*;
public class InspectGameCtlDrawSignatures extends GhidraScript {
 void fix(String addr,String convention,boolean varargs,int count)throws Exception{
  Function f=getFunctionAt(toAddr(addr));if(f==null)throw new Exception(addr);if(f.isThunk())f=f.getThunkedFunction(true);
  Parameter[] args=new Parameter[count];for(int i=0;i<count;i++)args[i]=new ParameterImpl("arg"+i,DWordDataType.dataType,currentProgram);
  f.setCallingConvention(convention);f.replaceParameters(Function.FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS,true,SourceType.USER_DEFINED,args);f.setVarArgs(varargs);println("signature "+f.getEntryPoint()+" "+f.getSignature());
 }
 public void run()throws Exception{
  fix("10014714","__thiscall",false,5);fix("1001478c","__cdecl",true,2);fix("100053f0","__cdecl",false,4);fix("10005300","__cdecl",false,9);
  DecompInterface d=new DecompInterface();d.openProgram(currentProgram);Path out=Path.of(getScriptArgs()[0]);Files.createDirectories(out);
  for(String at:new String[]{"100054c0","100063e0","10006830","100053f0","10005300"}){var f=getFunctionAt(toAddr(at));var r=d.decompileFunction(f,60,monitor);if(!r.decompileCompleted())throw new Exception(r.getErrorMessage());Files.writeString(out.resolve(at+".c"),r.getDecompiledFunction().getC());}d.dispose();
 }
}
