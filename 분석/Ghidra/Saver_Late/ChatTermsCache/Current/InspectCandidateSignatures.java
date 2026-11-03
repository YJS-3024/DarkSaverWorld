import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;
import ghidra.program.model.symbol.SourceType;
import java.nio.file.*;
public class InspectCandidateSignatures extends GhidraScript{
 void fix(Function f,String convention,int count,boolean variable,boolean returns)throws Exception{Parameter[] args=new Parameter[count];for(int i=0;i<count;i++)args[i]=new ParameterImpl("arg"+i,DWordDataType.dataType,currentProgram);f.setCallingConvention(convention);f.replaceParameters(Function.FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS,true,SourceType.USER_DEFINED,args);f.setVarArgs(variable);f.setReturnType(returns?DWordDataType.dataType:VoidDataType.dataType,SourceType.USER_DEFINED);}
 public void run()throws Exception{
  for(String at:new String[]{"00479850","00479856","004795a4","004795e6","004795da","00416ab0","004161e0","00415ba0"}){var f=getFunctionAt(toAddr(at));if(f==null)throw new Exception(at);if(f.isThunk())f=f.getThunkedFunction(true);int n=at.equals("00479850")?3:at.equals("00479856")?2:1;fix(f,"__thiscall",n,false,!at.equals("004161e0")&&!at.equals("00415ba0"));}
  for(var f:currentProgram.getFunctionManager().getExternalFunctions())if(f.getName().equals("sscanf"))fix(f,"__cdecl",2,true,true);
  Path out=Path.of(getScriptArgs()[0]);Files.createDirectories(out);DecompInterface d=new DecompInterface();d.openProgram(currentProgram);
  for(String at:new String[]{"004161e0","00415ba0"}){var f=getFunctionAt(toAddr(at));var r=d.decompileFunction(f,60,monitor);if(!r.decompileCompleted())throw new Exception(r.getErrorMessage());Files.writeString(out.resolve(at+".c"),r.getDecompiledFunction().getC());StringBuilder s=new StringBuilder();for(var ins:currentProgram.getListing().getInstructions(f.getBody(),true))s.append(ins.getAddress()+" "+ins+"\n");Files.writeString(out.resolve(at+".asm"),s.toString());}d.dispose();
 }
}
