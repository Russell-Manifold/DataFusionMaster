
namespace SBMS.Classes
{
    public class GetErrorResults
    {
       public string GetResultDesc(object strRCode)
        {
            string GetResultDescRet = default;
            switch (strRCode)
            {
                case "0":
                    {
                        GetResultDescRet = "0 = Call successfully executed" + '\r' + '\n';
                        break;
                    }

                case "1":
                    {
                        GetResultDescRet = "1 = File Not found" + '\r' + '\n';
                        break;
                    }

                case "2":
                    {
                        GetResultDescRet = "2 = Invalid number of fields" + '\r' + '\n';
                        GetResultDescRet = GetResultDescRet + "Check your SetDataPath call and make sure" + '\r' + '\n';
                        GetResultDescRet = GetResultDescRet + "pastel files exist at that path" + '\r' + '\n';
                        GetResultDescRet = GetResultDescRet + "Try and use directories less than 8 characters long" + '\r' + '\n';
                        break;
                    }

                case "3":
                    {
                        GetResultDescRet = "3 = Record update not successful" + '\r' + '\n';
                        break;
                    }

                case "4":
                    {
                        GetResultDescRet = "4 = Record insert not successful" + '\r' + '\n';
                        break;
                    }

                case "5":
                    {
                        GetResultDescRet = "5 = Record does not exist in file";
                        break;
                    }

                case "6":
                    {
                        GetResultDescRet = "6 = Data path does not exist";
                        break;
                    }

                case "7":
                    {
                        GetResultDescRet = "7 = Access denied";
                        break;
                    }

                case "9":
                    {
                        GetResultDescRet = "9 = End of file";
                        break;
                    }

                case "10":
                    {
                        GetResultDescRet = "10 = Field number specified not valid";
                        break;
                    }

                case "11":
                    {
                        GetResultDescRet = "11 = Invalid period number (1 to 13)";
                        break;
                    }

                case "12":
                    {
                        GetResultDescRet = "12 = Invalid Date";
                        break;
                    }

                case "13":
                    {
                        GetResultDescRet = "13 = Invalid account type (GDC)";
                        break;
                    }

                case "14":
                    {
                        GetResultDescRet = "14 = Invalid general ledger account number";
                        break;
                    }

                case "15":
                    {
                        GetResultDescRet = "15 = General ledger account contains sub accounts";
                        break;
                    }

                case "16":
                    {
                        GetResultDescRet = "16 = General ledger account number must be numeric";
                        break;
                    }

                case "17":
                    {
                        GetResultDescRet = "17 = Invalid customer account code";
                        break;
                    }

                case "18":
                    {
                        GetResultDescRet = "18 = Invalid supplier account code";
                        break;
                    }

                case "19":
                    {
                        GetResultDescRet = "19 = Invalid inventory item code";
                        break;
                    }

                case "20":
                    {
                        GetResultDescRet = "20 = Invalid salesman code";
                        break;
                    }

                case "21":
                    {
                        GetResultDescRet = "21 = Invalid job code";
                        break;
                    }

                case "22":
                    {
                        GetResultDescRet = "22 = Invalid Tax Type (0 to 30)";
                        break;
                    }

                case "23":
                    {
                        GetResultDescRet = "23 = Transaction amount cannot be less that the tax amount";
                        break;
                    }

                case "24":
                    {
                        GetResultDescRet = "24 = Invalid open item transaction type - must be O (Original) or A (Allocation)";
                        break;
                    }

                case "25":
                    {
                        GetResultDescRet = "25 = There cannot be more than 500 lines in a batch";
                        break;
                    }

                case "26":
                    {
                        GetResultDescRet = "26 = Invalid account description";
                        break;
                    }

                case "27":
                    {
                        GetResultDescRet = "27 = Default group needs to set up in Pastel";
                        break;
                    }

                case "28":
                    {
                        GetResultDescRet = "28 = Invalid document line type – must be 2, 5, or 7";
                        break;
                    }

                case "29":
                    {
                        GetResultDescRet = "29 = Invalid exclusive / inclusive – must be 0 or 1";
                        break;
                    }

                case "30":
                    {
                        GetResultDescRet = "30 = Invalid Entry Type (1 to 90)";
                        break;
                    }

                case "31":
                    {
                        GetResultDescRet = "31 = Duplicate inventory item";
                        break;
                    }

                case "32":
                    {
                        GetResultDescRet = "32 = Invalid multi-store code";
                        break;
                    }

                case "33":
                    {
                        GetResultDescRet = "33 = Invalid Currency Code";
                        break;
                    }

                case "99":
                    {
                        GetResultDescRet = "99 = General Error (the error message is the VB 5 error code and description)";
                        break;
                    }
            }

            return GetResultDescRet;
        }
    }
}
        
