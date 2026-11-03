import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;
import ghidra.program.model.symbol.SourceType;
import java.nio.file.*;
public class InspectMemoSignatures extends GhidraScript {
 void fix(String addr,String convention,int count)throws Exception{
  Function f=getFunctionAt(toAddr(addr));if(f==null)throw new Exception(addr);
  Parameter[] args=new Parameter[count];for(int i=0;i<count;i++)args[i]=new ParameterImpl("arg"+i,DWordDataType.dataType,currentProgram);
  f.setCallingConvention(convention);f.replaceParameters(Function.FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS,true,SourceType.USER_DEFINED,args);f.setVarArgs(false);
 }
 public void run()throws Exception{
  fix("00477e10","__cdecl",3);getFunctionAt(toAddr("00477e10")).setReturnType(DWordDataType.dataType,SourceType.USER_DEFINED);fix("00427d30","__thiscall",1);
  DecompInterface d=new DecompInterface();d.openProgram(currentProgram);Path out=Path.of(getScriptArgs()[0]);Files.createDirectories(out);
  for(String at:new String[]{"00477e10","00427d30"}){var f=getFunctionAt(toAddr(at));var r=d.decompileFunction(f,60,monitor);if(!r.decompileCompleted())throw new Exception(r.getErrorMessage());Files.writeString(out.resolve(at+".c"),r.getDecompiledFunction().getC());StringBuilder s=new StringBuilder();for(var ins:currentProgram.getListing().getInstructions(f.getBody(),true))s.append(ins.getAddress()+" "+ins+"\n");Files.writeString(out.resolve(at+".asm"),s.toString());}d.dispose();
 }
}
