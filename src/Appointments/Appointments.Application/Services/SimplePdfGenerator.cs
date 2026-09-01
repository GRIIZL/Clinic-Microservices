using System;
using System.Collections.Generic;
using System.Text;

namespace Appointments.Application.Services
{
    /// <summary>
    /// Генератор простого одностраничного PDF-документа (SRP: только формирование файла).
    /// Создаёт валидный PDF 1.4 с текстом в стандартном шрифте Helvetica.
    /// Текст должен быть латиницей: стандартные шрифты PDF не содержат кириллицы.
    /// </summary>
    public static class SimplePdfGenerator
    {
        public static byte[] Generate(string title, IEnumerable<(string Label, string Value)> lines)
        {
            // Экранируем служебные символы PDF и собираем строки контента
            var content = new StringBuilder();
            content.Append("BT /F1 16 Tf 60 780 Td (").Append(Escape(title)).Append(") Tj ET\n");

            var y = 750;
            foreach (var (label, value) in lines)
            {
                content.Append("BT /F1 11 Tf 60 ").Append(y)
                       .Append(" Td (").Append(Escape($"{label}: {value}")).Append(") Tj ET\n");
                y -= 20;
            }

            var stream = content.ToString();

            // Собираем PDF-файл по спецификации: заголовок, объекты, xref-таблица, trailer
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {stream.Length} >>\nstream\n{stream}endstream"
            };

            var pdf = new StringBuilder();
            pdf.Append("%PDF-1.4\n");

            var offsets = new List<int>();
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(pdf.Length);
                pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
            }

            var xrefPosition = pdf.Length;
            pdf.Append("xref\n0 ").Append(objects.Count + 1).Append("\n");
            pdf.Append("0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                pdf.Append(offset.ToString("D10")).Append(" 00000 n \n");
            }

            pdf.Append("trailer\n<< /Size ").Append(objects.Count + 1)
               .Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefPosition).Append("\n%%EOF");

            return Encoding.Latin1.GetBytes(pdf.ToString());
        }

        // Экранирование символов, запрещённых внутри PDF-строк
        private static string Escape(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
