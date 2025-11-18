using System;
using System.IO;
//using PasSDK;

namespace SBMS.Classes
{
    public class Authentication
    {
        //public static PastelPartnerSDK SDK = new PastelPartnerSDK();
        //public static PastelInventoryJnl InventSDK = new PastelInventoryJnl();
        //private string Serno = "DK198110007";
        private string Authcde = "5635796";
        ////////////////////////////////////////////////////////
        ///AUTHENTICATION
        public string GetAuthData()
        {
            try
            {
                string paths = AppDomain.CurrentDomain.BaseDirectory + "Paths.txt";               
                string s = File.ReadAllText(paths);
                byte[] data = System.Convert.FromBase64String(s.Split('|')[0]);
                string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(data);
                return returnValue+"|"+s.Split('|')[1] + "|" + s.Split('|')[2]; ;
            }
            catch (Exception e)
            {
                return  e+"";
            }
        }

        public object SetYourLicense()
        {
            //SDK.SetLicense(ref Serno, ref Authcde);
            return default(object);

        }

        public string SetDataPath(string datapath)
        {
            //string s = SDK.SetDataPath(datapath);
            return "";
        }

        public string SetGLPath(string glpath)
        {
            //string d = SDK.SetGLPath(glpath);
            return "";
        }

    }
}
        
