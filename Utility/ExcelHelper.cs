using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.ComponentModel;
using System.Reflection;


namespace Utility
{
    public class ExcelHelper
    {
        /// <summary>
        /// 產生 excel
        /// </summary>
        /// <typeparam name="T">傳入的物件型別</typeparam>
        /// <param name="data">物件資料集</param>
        /// <param name="optionalDisplayUserFieldHeaders">DisplayUser用的額外欄位名稱</param>
        /// <returns></returns>
        public XLWorkbook Export<T>(List<T> data, List<string> optionalDisplayUserFieldHeaders = null)
        {
            //建立 excel 物件
            XLWorkbook workbook = new XLWorkbook();
            //加入 excel 工作表名為 `Report`
            string sheetName = typeof(T).Name + " Report";
            var sheet = workbook.Worksheets.Add(sheetName);
            //欄位起啟位置
            int colIdx = 1;
            int optionalDisplayUserFieldCount = 0;
            //使用 reflection 將物件屬性取出當作工作表欄位名稱
            foreach (var item in typeof(T).GetProperties())
            {
                #region - 可以使用 DescriptionAttribute 設定，找不到 DescriptionAttribute 時改用屬性名稱 -
                //可以使用 DescriptionAttribute 設定，找不到 DescriptionAttribute 時改用屬性名稱
                DescriptionAttribute description = item.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (description != null)
                {
                    sheet.Cell(1, colIdx++).Value = description.Description;                    
                    continue;
                }
                sheet.Cell(1, colIdx++).Value = item.Name;
                #endregion
                #region - 直接使用物件屬性名稱 -
                //或是直接使用物件屬性名稱
                //sheet.Cell(1, colIdx++).Value = item.Name;
                #endregion

            }
            // 處理DisplayUser狀況
            if (typeof(T) == typeof(DisplayUser))
            {
                DisplayUser du = data.First() as DisplayUser;
                if (du != null && du.usageNumberArray != null)
                {
                    colIdx--;  // 覆蓋"操作"一欄
                    optionalDisplayUserFieldCount = du.usageNumberArray.Length;
                    for (int i = 0; i < optionalDisplayUserFieldCount; i++)
                    {
                        if (optionalDisplayUserFieldHeaders != null && optionalDisplayUserFieldHeaders.Count > i)  // 覆蓋名稱
                            sheet.Cell(1, colIdx).Value = optionalDisplayUserFieldHeaders[i];
                        else
                            sheet.Cell(1, colIdx).Value = "操作 #" + i.ToString();
                        colIdx++;
                    }
                }
            }
            else if (optionalDisplayUserFieldHeaders != null)
            {
                colIdx = 1;
                for (int i = 0; i < optionalDisplayUserFieldHeaders.Count; i++)
                {
                    sheet.Cell(1, colIdx).Value = optionalDisplayUserFieldHeaders[i];
                    colIdx++;
                }
            }
            //資料起始列位置
            int rowIdx = 2;
            foreach (var item in data)
            {
                //每筆資料欄位起始位置
                int conlumnIndex = 1;
                foreach (var jtem in item.GetType().GetProperties())
                {
                    //將資料內容加上 "'" 避免受到 excel 預設格式影響，並依 row 及 column 填入
                    sheet.Cell(rowIdx, conlumnIndex).Value = Convert.ToString(jtem.GetValue(item, null));//string.Concat("'", Convert.ToString(jtem.GetValue(item, null)));
                    conlumnIndex++;
                }
                // 處理DisplayUser狀況
                if (optionalDisplayUserFieldCount > 0)
                {
                    DisplayUser du = item as DisplayUser;
                    if (du != null && du.usageNumberArray != null)
                    {
                        conlumnIndex--;  // 覆蓋"操作"一欄
                        for (int i = 0; i < optionalDisplayUserFieldCount; i++)
                        {
                            sheet.Cell(rowIdx, conlumnIndex + i).Value = du.usageNumberArray[i];
                        }
                    }
                }
                rowIdx++;
            }
            return workbook;
        }

        public void AddExport<T>(XLWorkbook workbook, List<T> data)
        {
            //加入 excel 工作表名為 `Report`
            string sheetName = typeof(T).Name + " Report";
            var sheet = workbook.Worksheets.Add(sheetName);
            //欄位起啟位置
            int colIdx = 1;
            //使用 reflection 將物件屬性取出當作工作表欄位名稱
            foreach (var item in typeof(T).GetProperties())
            {
                #region - 可以使用 DescriptionAttribute 設定，找不到 DescriptionAttribute 時改用屬性名稱 -
                //可以使用 DescriptionAttribute 設定，找不到 DescriptionAttribute 時改用屬性名稱
                DescriptionAttribute description = item.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (description != null)
                {
                    sheet.Cell(1, colIdx++).Value = description.Description;
                    continue;
                }
                sheet.Cell(1, colIdx++).Value = item.Name;
                #endregion
                #region - 直接使用物件屬性名稱 -
                //或是直接使用物件屬性名稱
                //sheet.Cell(1, colIdx++).Value = item.Name;
                #endregion

            }
            //資料起始列位置
            int rowIdx = 2;
            foreach (var item in data)
            {
                //每筆資料欄位起始位置
                int conlumnIndex = 1;
                foreach (var jtem in item.GetType().GetProperties())
                {
                    //將資料內容加上 "'" 避免受到 excel 預設格式影響，並依 row 及 column 填入
                    sheet.Cell(rowIdx, conlumnIndex).Value = Convert.ToString(jtem.GetValue(item, null));//string.Concat("'", Convert.ToString(jtem.GetValue(item, null)));
                    conlumnIndex++;
                }
                rowIdx++;
            }
        }
    }
}
