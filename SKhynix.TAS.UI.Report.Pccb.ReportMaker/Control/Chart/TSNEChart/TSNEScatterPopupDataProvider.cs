using System.Data;
using System.Threading.Tasks;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public interface ITSNEScatterPopupDataProvider
    {
        string SourceDescription { get; }
        Task<DataTable> LoadAllAsync();
    }
}





