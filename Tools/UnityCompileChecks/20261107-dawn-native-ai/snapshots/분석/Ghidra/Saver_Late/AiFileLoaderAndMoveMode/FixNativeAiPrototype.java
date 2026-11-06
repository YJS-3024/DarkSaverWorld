import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;
import ghidra.program.model.symbol.SourceType;
public class FixNativeAiPrototype extends GhidraScript {
 public void run()throws Exception {
  var f=getFunctionAt(toAddr("10003480"));println("TEMPORARY_OLD_PROTOTYPE "+f.getPrototypeString(true,true));
  var integer=IntegerDataType.dataType;var pointer=new PointerDataType(integer);Parameter[] ps=new Parameter[7];String[] names={"output","unit","cursor","centerX","centerY","radius","mode"};
  for(int i=0;i<7;i++)ps[i]=new ParameterImpl(names[i],i==0||i==2?pointer:integer,currentProgram);
  f.setCallingConvention("__cdecl");f.replaceParameters(Function.FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS,true,SourceType.USER_DEFINED,ps);f.setReturnType(pointer,SourceType.USER_DEFINED);
  println("TEMPORARY_NEW_PROTOTYPE "+f.getPrototypeString(true,true));
 }
}
