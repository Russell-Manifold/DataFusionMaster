<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSPurchaseOrdersM.aspx.cs" Inherits="SBMS.OSPurchaseOrdersM" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OS Purchase Orders</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="../images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="../SBMSMobile/css/main.css" />
    <!-- ---------------------------------------------------------------------------------- -->
    <!-- PWA Manifest -->
    <link rel="manifest" href="../SBMSMobile/manifest.json"/>
    <!-- Theme Color -->
    <meta name="theme-color" content="#3367D6"/>
    <!-- iOS PWA Support -->
    <meta name="apple-mobile-web-app-capable" content="yes"/>
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent"/>
    <meta name="apple-mobile-web-app-title" content="Data Fusion"/>    
    <!-- Icons for iOS -->
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png"/>    
    <!-- Disable phone number detection -->
    <meta name="format-detection" content="telephone=no"/>
    
    <!-- CSS for fullscreen app feel -->
    <style>
        html, body {
            height: 100%;
            margin: 0;
            padding: 0;
            overflow: auto;
            -webkit-overflow-scrolling: touch;
        }
        
        /* Prevent zoom on input focus for better app feel */
        input, select, textarea {
            font-size: 16px;
        }
        
        /* Hide browser UI on mobile */
        @media screen and (max-width: 736px) {
            body {
                min-height: 100vh;
                min-height: -webkit-fill-available;
            }
        }
    </style>
    <!-- ---------------------------------------------------------------------------------- -->

</head>
<body class="is-preload">
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>       
           <div id="main">
        <div class="content">
            <div class="container">
                            <div class="row 150%"> 
                                <div class="col-12 col-12-normal" style="text-align: center">
                                    <asp:LinkButton ID="lbtnHome" runat="server" CssClass="buttonM icon solid fa-home" OnClick="lbtnHome_Click" style="float:left"></asp:LinkButton>    
                                       <h4>Purchase Orders<asp:Label ID="lblpoqty" runat="server" Text=""></asp:Label></h4>
                                </div>
                                </div>
                          <div class="row 150%">
                              <div class="col-12 col-12-mobile">
                                  <div style="display: none"><asp:Label ID="lblDir" runat="server" Text=""></asp:Label></div>
                                  <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" Style="font-size: 1em; width: 100%">
                                  <table style="width: 100%">
                                      <tr>
                                          <td>Find&nbsp;<asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton></td>
                                      </tr>
                                  </table>
                                  </asp:Panel>
                              </div>
                          </div>
                            <div class="row 150%">
                                <div class="col-12 col-12-normal">
                                    <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnSelectedIndexChanged="GridPOs_SelectedIndexChanged" AllowSorting="true" OnSorting="GridPOs_Sorting">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                        <Columns>
                                            <asp:BoundField DataField="DocGUID" ReadOnly="True" />
                                            <asp:BoundField HeaderText="PO Number" DataField="DocumentNumber" ReadOnly="True" SortExpression="DocumentNumber"/>
                                            <asp:BoundField HeaderText="Supplier Name" DataField="CustSupName" ReadOnly="True" SortExpression="CustSupName" ItemStyle-Width="100%" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True" SortExpression="Reference" />
                                            <asp:BoundField HeaderText="Due_Date" DataField="DueDelDate" ReadOnly="True" DataFormatString="{0:dd/MM/yyyy}" SortExpression="DueDelDate" />
                                           <asp:TemplateField HeaderText="Started" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em" SortExpression="RecStarted">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkstarted" runat="server" Checked='<%# Eval("Started")%>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                            </div>
                         </div>         
            <section id="footer" class="wrapper">
    &nbsp;
    </section>
        </div>
    </form>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
