using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static ProductRecomendation.Pages.RecommendationSystemModel;

namespace egitlab_PotionNetCore.Data
{

    public class PdfHtmlGenerator
    {
        public static string GetHtmlString(List<ListExport> listExports, string path, string shipTo, string recDate)
        {

            var getData = listExports;

            int splitmonth = int.Parse(recDate.Split('-')[0]);
            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);

            string recomendationDateString = month + " - " + recDate.Split('-')[1];

            var sb = new StringBuilder();

            sb.AppendLine("</br><table class='table table-borderless'>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<td class='text-left'>"); sb.AppendLine($"<img src=\"{path}\\images\\PDF\\logoenseval.png\" width='120px'/>"); sb.AppendLine("</td>");
            sb.AppendLine("<td class='text-left' style='font-size:18px; font-weight:600;' >"); sb.AppendLine($"<img src=\"{path}\\images\\PDF\\namaEnseval.png\" width='500px'/><div>Jl. Pulo Lentut No 10</div><div>Kawasan Industri Pulo Gadung, Jakarta Timur 13920</div>"); sb.AppendLine("</td>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("</table></br></br>");

            sb.Append(@"<style>
                        td {
                          height: 40px;
                        }
                        </style>");

            sb.Append(@"
                    <html>
                        <head>
                        </head>");

            sb.AppendLine($"<body>");



            sb.AppendFormat(@"<table class='table-borderless mb-2 mt-3'>
                        <tbody style='font-size:15px;'>
                            <tr>
                                <th class='text-left'>Customer</th>
                                <th class='text-left'>&nbsp;&nbsp;:&nbsp;&nbsp;{0}</th>
                            </tr>
                            <tr>
                                <th class='text-left'>Recommendation Date</th>
                                <th class='text-left'>&nbsp;&nbsp;:&nbsp;&nbsp;{1}</th>
                            </tr>
                            <tr>
                                <th class='text-left'>Download Date</th>
                                <th class='text-left'>&nbsp;&nbsp;:&nbsp;&nbsp;{2}</th>
                            </tr>
                        </tbody>
                    </table>", shipTo, recomendationDateString, DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"));



            sb.Append(@"       <table class='table table-bordered' width='100%' cellspacing='0'>
                                    <tr class='text-center' style='font-size:15px;'>
                                                <th class='px-1 py-3'>No</th>
                                                <th class='py-3'>Item Code</th>
                                                <th class='py-3'>Item Desc</th>
                                    </tr>          
                                <tbody style='font-size:14px;'>");
            var iterator = 1;
            foreach (var item in getData)
            {
                sb.AppendFormat(@"<tr>
                                    <td class='text-center align-middle'>{0}</td>
                                    <td class='text-center  align-middle'>{1}</td>
                                    <td class='text-center px-1 align-middle'>{2}</td>


                                  </tr>", iterator, item.item_code, item.item_desc);
                iterator++;
            }
            sb.AppendFormat(@"</tbody> </table>");


            sb.Append(@"  </body>
                        </html> ");






            return sb.ToString();
        }



       



    }
}
