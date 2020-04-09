using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Exports.ExportTxtCustom
{
    public class ExportTxtZucchettiSolaris
    {
        private const string _separator = ";";

        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly ICollection<string> _txtLines;

        public ExportTxtZucchettiSolaris(DateTime from, DateTime to)
        {
            _from = from;
            _to = to;
            _txtLines = new List<string>();
        }

        public void LaunchExport()
        {
            var regs = RepoManager.Reg_VRepo.DbSet.Where(reg => reg.Data_Ora_Fig_E >= _from
                                                             && reg.Data_Ora_Fig_U <= _to
                                                             && reg.Durata_Fig.HasValue
                                                             && reg.Cant_Id.HasValue
                                                             && reg.Col_Id.HasValue).ToList();

            var centriDiCosto = regs.Select(reg => reg.CentroDiCosto_Id).Distinct().ToList();

            var centriDiCostoD = RepoManager.CentroDiCostoRepo.DbSet
                                                              .Where(c => centriDiCosto.Contains(c.CentroDiCosto_Id))
                                                              .ToDictionary(c => c.CentroDiCosto_Id);

            foreach (var reg in regs)
            {
                var builder = new StringBuilder();

                var centroDiCosto = (CentroDiCosto)null;

                if (reg.CentroDiCosto_Id.HasValue)
                    centriDiCostoD.TryGetValue(reg.CentroDiCosto_Id.Value, out centroDiCosto);

                builder = builder.Append(reg.Col_Mnemonic.Trim()).Append(_separator);
                builder = builder.Append(reg.Data_Reg.Value.ToString("yyyyMMdd")).Append(_separator);
                builder = builder.Append(centroDiCosto != null ? centroDiCosto.Codice : "").Append(_separator);
                builder = builder.Append(reg.Cant_Mnemonic).Append(_separator);
                builder = builder.Append(reg.Data_Ora_Fig_E.Value.TimeOfDay.ToString("hhmm")).Append(_separator);
                builder = builder.Append(reg.Data_Ora_Fig_U.Value.TimeOfDay.ToString("hhmm")).Append(_separator);
                builder = builder.Append(reg.Durata_Fig).Append(_separator);

                _txtLines.Add(builder.ToString());
            }
        }

        public void ExportToResponse()
        {
            HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=" + "Solaris-Zucchetti.csv");
            HttpContext.Current.Response.ContentType = "text/plain";

            using (StreamWriter writer = new StreamWriter(HttpContext.Current.Response.OutputStream))
            {
                foreach (var line in _txtLines)
                {
                    writer.WriteLine(line);
                }

            }
            // Conclude la connessione
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
    }
}
