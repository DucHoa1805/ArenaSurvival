using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaSurvival.Data
{
    public class CsvException : Exception
    {
        public CsvException(string message) : base(message) { }
    }

    /// <summary>
    /// Đọc CSV xuất từ Google Sheets / Excel (hàm thuần, có unit test). Dòng đầu là tên cột; mỗi dòng sau thành 1 từ điển cột → giá trị
    /// (đã trim). Hỗ trợ ngoặc kép, dấu phẩy và xuống dòng trong ngoặc, "" thành ", BOM, CRLF; bỏ dòng trống.
    /// Không dùng thư viện ngoài: bảng cân bằng nhỏ, và parser tự viết thì báo lỗi kèm số dòng cho người sửa bảng.
    /// </summary>
    public static class CsvReader
    {
        /// <summary>Cột ẩn thêm vào mỗi dòng: số dòng trong file (để báo lỗi đúng chỗ).</summary>
        public const string LineKey = "__line";

        public static List<Dictionary<string, string>> Parse(string text)
        {
            var result = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(text)) return result;
            if (text[0] == '﻿') text = text.Substring(1);

            List<(int line, List<string> cells)> records = SplitRecords(text);
            if (records.Count == 0) return result;

            List<string> header = records[0].cells;
            for (int i = 0; i < header.Count; i++) header[i] = header[i].Trim();

            for (int r = 1; r < records.Count; r++)
            {
                (int line, List<string> cells) = records[r];
                if (cells.Count > header.Count)
                    throw new CsvException($"CSV dòng {line}: có {cells.Count} ô nhưng chỉ có {header.Count} cột.");

                var row = new Dictionary<string, string>(StringComparer.Ordinal) { [LineKey] = line.ToString() };
                for (int c = 0; c < header.Count; c++) row[header[c]] = c < cells.Count ? cells[c].Trim() : "";
                result.Add(row);
            }
            return result;
        }

        private static List<(int, List<string>)> SplitRecords(string text)
        {
            var records = new List<(int, List<string>)>();
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            int line = 1, recordLine = 1;

            void EndRecord()
            {
                cells.Add(cell.ToString());
                cell.Clear();
                // Dòng toàn ô trống (kể cả "   ") coi như dòng trống: Sheets hay để lại ở cuối file.
                bool blank = true;
                foreach (string c in cells) if (c.Trim().Length > 0) { blank = false; break; }
                if (!blank) records.Add((recordLine, cells));
                cells = new List<string>();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else quoted = false;
                    }
                    else
                    {
                        if (ch == '\n') line++;
                        cell.Append(ch);
                    }
                    continue;
                }

                switch (ch)
                {
                    case '"': quoted = true; break;
                    case ',': cells.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n':
                        EndRecord();
                        line++;
                        recordLine = line;
                        break;
                    default: cell.Append(ch); break;
                }
            }

            if (quoted) throw new CsvException($"CSV dòng {recordLine}: thiếu dấu \" đóng.");
            if (cell.Length > 0 || cells.Count > 0) EndRecord();
            return records;
        }
    }
}
