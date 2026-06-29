using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    public interface IPcaScatterPopupDataProvider
    {
        string SourceDescription { get; }
        Task<DataTable> LoadAllAsync();
    }

    /// <summary>
    /// Popup-internal data provider used until the company service call is wired in.
    /// Replace this class with a service-backed provider that returns the same columns:
    /// DRAFT_NO, PARAM_TYP, ENGR_RSLT_VAL, CONV_EXPER_CTN.
    /// </summary>
    public sealed class PcaScatterVirtualDatabaseDataProvider : IPcaScatterPopupDataProvider
    {
        private readonly int seed;

        public PcaScatterVirtualDatabaseDataProvider()
            : this(20260628)
        {
        }

        public PcaScatterVirtualDatabaseDataProvider(int seed)
        {
            this.seed = seed;
            ResponseCount = 180;
            DefectCount = 180;
            EpmCount = 20;
            ProbeCount = 20;
        }

        public int ResponseCount { get; set; }
        public int DefectCount { get; set; }
        public int EpmCount { get; set; }
        public int ProbeCount { get; set; }

        public string SourceDescription
        {
            get { return "팝업 내부 가상 DB 조회"; }
        }

        public Task<DataTable> LoadAllAsync()
        {
            return Task.Run(delegate
            {
                PcaExadataSnapshot snapshot = new PcaExadataSampleDataFactory(seed)
                    .CreateDatabaseLikeSnapshot(
                        ResponseCount,
                        DefectCount,
                        EpmCount,
                        ProbeCount);
                return ToDataTable(snapshot);
            });
        }

        public static DataTable ToDataTable(PcaExadataSnapshot snapshot)
        {
            DataTable table = new DataTable("PCCB_INFER_RSLT_INF");
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("ENGR_RSLT_VAL", typeof(string));
            table.Columns.Add("CONV_EXPER_CTN", typeof(string));
            table.Columns.Add("CHG_TM", typeof(DateTime));

            if (snapshot == null || snapshot.Rows == null)
            {
                return table;
            }

            foreach (PcaExadataSourceRow row in snapshot.Rows)
            {
                DataRow dataRow = table.NewRow();
                dataRow["DRAFT_NO"] = row.DraftNo;
                dataRow["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(row.ParameterType);
                dataRow["ENGR_RSLT_VAL"] = row.LabelY;
                dataRow["CONV_EXPER_CTN"] = row.RawConvExperimentJson;
                dataRow["CHG_TM"] = DateTime.Now;
                table.Rows.Add(dataRow);
            }

            return table;
        }
    }
}
