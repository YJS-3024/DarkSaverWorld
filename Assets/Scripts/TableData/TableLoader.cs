using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TableLoader
{
    public void LoadTable<T>(string tableName, ref Dictionary<int, T> dic) where T : new()
    {
        var strData = GetBinaryTable(tableName);
        var lineDatas = strData.Split(new char[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
        if (lineDatas.Length == 0)
            return;

        var variables = lineDatas[0].Split(',');
        for (int i = 1; i < lineDatas.Length; i++)
        {
            if (lineDatas[i].Equals(""))
                continue;

            var values = lineDatas[i].Split(',');

            //  ID 조차 없다.
            if (string.IsNullOrEmpty(values.FirstOrDefault()))
                continue;

            var data = new T();
            var type = data.GetType();
            var fieldList = type.GetFields().ToList();
            for (var v = 0; v < variables.Length; v++)
            {
                //  데이터 필드들
                var strVariable = variables[v];
                var variable = strVariable.Split('_').LastOrDefault();

                //  필드가 공백
                if (string.IsNullOrEmpty(variable))
                    continue;

                //  공백 및 잘못된 값에 대한 에러 표시
                if (string.IsNullOrEmpty(values[v]))
                {
                    Debug.LogError($"{tableName} 테이블의 {variables[0]}:{values[0]} 라인에 타입 {variable}값이 잘못되었습니다.");
                    continue;
                }

                //  데이터 필드의 인덱스
                var index = fieldList.FindIndex(x => x.Name == variable);
                if (index >= 0)
                {
                    var fieldType = fieldList[index].FieldType;                 //  필드형
                    var value = GetCheckTypeValue(fieldType, values[v]);  //  값
                    type.GetField(variable).SetValue(data, value);          //  해당 필드에 값 셋팅
                }

                //  데이터 리스트로 정리
                var key = Convert.ToInt32(values[0]);
                dic[key] = data;
            }
        }
    }

    private string GetBinaryTable(string fileName)
    {

        var strResult = string.Empty;
        var filePath = $"Assets/Resources/Tables/{fileName}.binary";
        if (File.Exists(filePath) == false)
            return strResult;

        using (FileStream fs = new FileStream(filePath,FileMode.Open))
        {
            using (BinaryReader br = new BinaryReader(fs))
            {
                strResult = br.ReadString();
            }
        }

        return strResult;
    }

    private object GetCheckTypeValue(Type type, object value)
    {
        try
        {
            if (type == typeof(bool))
            {
                if (string.IsNullOrEmpty(value.ToString()))
                    return string.Empty;

                var isO = Convert.ToInt16(value);
                return isO > 0;
            }
            else
            {
                if (string.IsNullOrEmpty(value.ToString()))
                {
                    Debug.LogWarning($"에러 타입{type} 값 {value}");
                    return 0;
                }

                return Convert.ChangeType(value, type);
            }
        }
        catch (Exception e)
        {
            return -1;
            throw;
        }
    }
}

public class BaseTableData
{
    public virtual void ParseData(string[] arrVariables, string[] arrValues)
    {
    }
}
