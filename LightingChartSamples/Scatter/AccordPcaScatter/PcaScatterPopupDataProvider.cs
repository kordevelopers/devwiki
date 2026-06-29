using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter
{
    public interface IPcaScatterPopupDataProvider
    {
        string SourceDescription { get; }
        Task<DataTable> LoadAllAsync();
    }

    /// <summary>
    /// 실제 서비스 연결 전까지 사용하는 샘플 데이터 공급자다.
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
            get { return "Virtual in-memory database"; }
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

