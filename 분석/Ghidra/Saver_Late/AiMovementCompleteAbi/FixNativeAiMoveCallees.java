import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;
import ghidra.program.model.symbol.SourceType;
public class FixNativeAiMoveCallees extends GhidraScript {
 public void run()throws Exception {
  var integer=IntegerDataType.dataType;var pointer=new PointerDataType(integer);
  for(String at:new String[]{"100030b0","10002ee0"}){
   var f=getFunctionAt(toAddr(at));println("TEMPORARY_OLD_PROTOTYPE "+f.getPrototypeString(true,true));
   Parameter[] ps=new Parameter[5];String[] names={"output","unit","centerX","centerY","radius"};
   for(int i=0;i<5;i++)ps[i]=new ParameterImpl(names[i],i==0?pointer:integer,currentProgram);
   f.setCallingConvention("__cdecl");f.replaceParameters(Function.FunctionUpdateType.DYNAMIC_STORAGE_ALL_PARAMS,true,SourceType.USER_DEFINED,ps);f.setReturnType(pointer,SourceType.USER_DEFINED);
   println("TEMPORARY_NEW_PROTOTYPE "+f.getPrototypeString(true,true));
  }
 }
}
