using System;

namespace SBMS.Classes
{
    public class UserDetails
    {
        public string UserName { get; set; }
        public bool UseGenericLogin { get; set; }
        public string LoginName { get; set; }
        public string LoginPwd { get; set; }
        public string LoginEncrypted { get; set; }
        public long CoID { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public bool CanReceive { get; set; }
        public bool CanTransfer { get; set; }
        public bool CanViewPickSlips { get; set; }
        public bool CanTrackPickSlips { get; set; }
        public bool CanSalesForecast { get; set; }
        public bool CanViewJobCards { get; set; }
        public bool CanTrackJobCards { get; set; }
        public bool CanSeeFGDemands { get; set; }
        public bool NotifyGRN { get; set; }
        public bool NotifyTransfer { get; set; }
        public bool NotifySOComplete { get; set; }
        public bool NotifyPSMove { get; set; }
        public bool NotifyJCMove { get; set; }
        public bool NotifyProdMove { get; set; }
        public bool CanCreateBomKit { get; set; }
        public System.Guid UserGuiD { get; set; }
        public bool UseModule2 { get; set; }
        public bool UseModule3 { get; set; }
        public bool UATMode { get; set; }
        public bool SendMessages { get; set; }
        public bool CanEditLotNumbers { get; set; }
        public bool CompanyUseLotNumbers { get; set; }
        public bool CompanyUseLotAddDetails { get; set; }
        public bool CompanyAllowSystemLotNumbers { get; set; }
        public bool AllowScannerCount { get; set; }
        public bool AllowScannerReceive { get; set; }
        public bool AllowScannerPutAway { get; set; }
        public string BinSegments { get; set; }
        public string BinDelimiter { get; set; }
        public int CompanyDecPlaces { get; set; }

        public bool CanStockControl { get; set; }
        public bool CanViewWorksOrders { get; set; }
        public bool CanFillWorksOrders { get; set; }
        public bool CanViewRMD { get; set; }
        public bool isSuperUser { get; set; }
        public bool UseAutoManf { get; set; }
        public bool UsePickSlipTracking { get; set; }
        public bool UseBarcodes { get; set; }
        public bool MobileModule { get; set; }
        public bool AutoUpdateSageSOs { get; set; }
        public bool AutoGenTaxInvoice { get; set; }
        public string LoggedInSessionID { get; set; }
        public bool UsePacks { get; set; }
        public DateTime ExpiryDate { get; set; }

        public bool ShowManfCosts { get; set; }

        public string SageWeightField { get; set; }
    }
} 
