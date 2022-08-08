using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Utilities;
using System.Collections;
using System.Xml;
using updateCliente.WsDynamicsAx;

namespace updateCliente
{
    public class UpdateCliente : IPlugin
    {

        private string organizacion;
        //private string DomainLogonName;
        private string DomainLogonName = "BEPENSA\\manguas";
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            organizacion = context.OrganizationName;

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity preImage = null;
                Entity postImage = null;

                IOrganizationServiceFactory ICrm = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService serviceProxy = ICrm.CreateOrganizationService(context.UserId);

                string[] attributes = new string[] {   "accountnumber", "name", "fib_rfc", "address1_line1", "fib_coloniaid", "address1_postalcode",
                                                "address2_line1", "fib_coloniaid2", "address2_postalcode", "fib_address3_line1", "fib_coloniaid3",
                                                "fib_address3_postalcode", "fib_apellidopaterno", "fib_apellidomaterno", "address1_primarycontactname",
                                                "fib_segundonombre", "telephone3", "fax", "emailaddress1", "fib_impuestoaplicable",
                                                    "fib_numpersonafisica", "lastname", "fib_apellidomaterno", "firstname", "middlename", "fib_rfc",
                                                "fib_curp","address1_line1","fib_coloniaid","address1_postalcode", 
                                                "fib_address3_line1","fib_coloniaid3","fib_address3_postalcode",
                                                "fib_address4_line1","fib_coloniaid4","fib_address4_postalcode",
                                                "telephone2","mobilephone","emailaddress1","fib_impuestosaplicables","customertypecode",
                                                    "fib_estatus", "fib_formadepagoid", "fib_digitos", "fib_correoelectronicoadicional","fib_paisdenacimientoid",
                                                    "fib_usocomprobanteid", "fib_regimenfiscalid", "fib_codigopostalcfdi", "fib_razonsocialcfdi", "gendercode"
                                                }; // RGomezS - 4 dígitos - 27/06/2013

                Entity entity = (Entity)context.InputParameters["Target"];

                //Se filtran los atributos modificados, si no se ha cambiado alguno de la lista se detiene la ejecución del Plugin
                if (context.PreEntityImages.Contains("Images") && context.PreEntityImages["Images"] is Entity)
                    preImage = (Entity)context.PreEntityImages["Images"];

                if (context.PostEntityImages.Contains("Images") && context.PostEntityImages["Images"] is Entity)
                    postImage = (Entity)context.PostEntityImages["Images"];
                
                DifferenceCollection differences = Utilities.Utilities.getDifferences(preImage, postImage);
                bool changed = false;

                for (int i = 0; i < attributes.Length; i++)
                {
                    if (differences.Contains(attributes[i]))
                    {
                        changed = true;
                        break;
                    }
                }

                if (!changed)
                    return;

                try
                {
                    setDomainLogonName(serviceProxy, context);
                    insertInAx(entity.Id, serviceProxy, entity.LogicalName);
                }
                catch (System.Web.Services.Protocols.SoapException ex)
                {
                    //Delegar error
                    throw new InvalidPluginExecutionException("Error: ", ex);
                }
                catch (Exception ex)
                {
                    //Delegar error
                    throw new InvalidPluginExecutionException("Error: " + ex.Message);
                }
            }
        }

        public string insertInAx(Guid idCliente, IOrganizationService servicio, String nomEntidad)
        {
            string resultado = "";
            String nomcliente = "", codigocliente = "", rfc = "", curp = "", calle = "", colonia = "", cp = "", calle2 = "", colonia2 = "", cp2 = "", calle3 = "", colonia3 = "", cp3 = "", conClie = "",
                   nombrecontacto = "", telefono1 = "", telefono2 = "", email = "", impuestos = "", nomdir1 = "Dirección Fiscal", nomdir2 = "Dirección Adicional 1", nomdir3 = "Dirección Adicional 2",
                   nom1 = "", nom2 = "", apellidop = "", apellidom = "", formapago = "", digitos = "", email2 = "", nacionalidad = "",//JCEn 14/05/2015 // RGomezS - 05/07/2013
                   usoComprobanteCodigo = string.Empty, regimenFiscalCodigo = string.Empty, codigoPostal = string.Empty, razonSocial = string.Empty, genero = string.Empty;

            Guid idcolonia, idlinea, idformapago;
            idlinea = new Guid();

            string[] atributos = new string[] { "fib_lineadecreditoid", "fib_customerpfid", "fib_customerpmid", "fib_estatus" };

            string cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                      "      <entity name='fib_lineadecredito'>" +
                      "          <attribute name='fib_lineadecreditoid' />" +
                      "          <attribute name='fib_customerpfid' />" +
                      "          <attribute name='fib_customerpmid' />" +
                      "          <attribute name='fib_estatus' />" +
                      "		     <filter type='and'>";
            if (nomEntidad == "account")
            {
                cadFetch += "              <condition attribute='fib_customerpmid' operator='eq' value='" + idCliente.ToString() + "' />";
            }
            else
            {
                cadFetch += "              <condition attribute='fib_customerpfid' operator='eq' value='" + idCliente.ToString() + "' />";
            }
            cadFetch += "          </filter>" +
                      "      </entity>" +
                      "  </fetch>";
            var request = new ExecuteFetchRequest { FetchXml = cadFetch };
            var response = (ExecuteFetchResponse)servicio.Execute(request);

            string result = response.FetchXmlResult;
            List<Hashtable> EntidadLC = this.XmlToMap(result, atributos);

            Int32 bdisposiciones = 0;
            if (EntidadLC != null)
            {
                foreach (Hashtable entity in EntidadLC)
                {
                    Hashtable CurrentLC = entity;
                    if (CurrentLC["fib_estatus"] != null)
                    {
                        if (int.Parse(CurrentLC["fib_estatus"].ToString()) == 2)
                        {
                            bdisposiciones += 1;
                            idlinea = (new Guid(CurrentLC["fib_lineadecreditoid"].ToString()));
                        }
                    }
                }
            }
            if (bdisposiciones > 0)
            {
                if (nomEntidad == "account")
                {
                    atributos = new string[] {"accountnumber", "name", "fib_rfc", "address1_line1", "fib_coloniaid", "address1_postalcode", 
                                                "address2_line1","fib_coloniaid2","address2_postalcode","fib_address3_line1","fib_coloniaid3",
                                                "fib_address3_postalcode","fib_apellidopaterno","fib_apellidomaterno","address1_primarycontactname",
                                                "fib_segundonombre","telephone3","fax","emailaddress1","fib_impuestoaplicable",
                                                "fib_formadepagoid","fib_digitos","fib_correoelectronicoadicional", // RGomezS 
                                                "fib_usocomprobanteid", "fib_regimenfiscalid", "fib_codigopostalcfdi", "fib_razonsocialcfdi"}; 

                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='account'>" +
                                  "          <attribute name='accountnumber' />" +
                                  "          <attribute name='name' />" +
                                  "          <attribute name='fib_rfc' />" +
                                  "          <attribute name='address1_line1' />" +
                                  "          <attribute name='fib_coloniaid' />" +
                                  "          <attribute name='address1_postalcode' />" +
                                  "          <attribute name='address2_line1' />" +
                                  "          <attribute name='fib_coloniaid2' />" +
                                  "          <attribute name='address2_postalcode' />" +
                                  "          <attribute name='fib_address3_line1' />" +
                                  "          <attribute name='fib_coloniaid3' />" +
                                  "          <attribute name='fib_address3_postalcode' />" +
                                  "          <attribute name='fib_apellidopaterno' />" +
                                  "          <attribute name='fib_apellidomaterno' />" +
                                  "          <attribute name='address1_primarycontactname' />" +
                                  "          <attribute name='fib_segundonombre' />" +
                                  "          <attribute name='telephone3' />" +
                                  "          <attribute name='fax' />" +
                                  "          <attribute name='emailaddress1' />" +
                                  "          <attribute name='fib_impuestoaplicable' />" +
                                  "          <attribute name='fib_formadepagoid' />" +
                                  "          <attribute name='fib_digitos' />" +
                                  "          <attribute name='fib_correoelectronicoadicional' />" +
                                  "          <attribute name='fib_tipodepersonamoral' />" +
                                  "          <attribute name='fib_usocomprobanteid' />" +
                                  "          <attribute name='fib_regimenfiscalid' />" +
                                  "          <attribute name='fib_codigopostalcfdi' />" +
                                  "          <attribute name='fib_razonsocialcfdi' />" +
                                  "		     <filter type='and'>" +
                                  "              <condition attribute='accountid' operator='eq' value='" + idCliente.ToString() + "' />" +
                                  "          </filter>" +
                                  "      </entity>" +
                                  "  </fetch>";
                    request = new ExecuteFetchRequest { FetchXml = cadFetch };
                    response = (ExecuteFetchResponse)servicio.Execute(request);

                    result = response.FetchXmlResult;
                    List<Hashtable> cliente = this.XmlToMap(result, atributos);
                    //JCEN 14/05/2015
                    if (cliente[0]["fib_tipodepersonamoral"] != null)
                    {
                        if (cliente[0]["fib_tipodepersonamoral"].ToString() == "1")
                            conClie = "04";
                        else if (cliente[0]["fib_tipodepersonamoral"].ToString() == "2")
                            conClie = "05";
                        else if (cliente[0]["fib_tipodepersonamoral"].ToString() == "3")
                            conClie = "06";
                    }
                    else
                    {
                        conClie = "04";
                    }
                    nacionalidad = "MX";
                    ////// JCEN
                    //nomcliente = cliente.name != null ? cliente.name : "";
                    nomcliente = cliente[0]["name"] != null ? cliente[0]["name"].ToString() : "";
                    //rfc = cliente.fib_rfc != null ? cliente.fib_rfc : "";
                    rfc = cliente[0]["fib_rfc"] != null ? cliente[0]["fib_rfc"].ToString() : "";
                    //codigocliente = cliente.accountnumber != null ? cliente.accountnumber : "";
                    codigocliente = cliente[0]["accountnumber"] != null ? cliente[0]["accountnumber"].ToString() : "";
                    //calle = cliente.address1_line1 != null ? cliente.address1_line1 : "";
                    calle = cliente[0]["address1_line1"] != null ? cliente[0]["address1_line1"].ToString() : "";
                    //calle2 = cliente.address2_line1 != null ? cliente.address2_line1 : "";
                    calle2 = cliente[0]["address2_line1"] != null ? cliente[0]["address2_line1"].ToString() : "";
                    //calle3 = cliente.fib_address3_line1 != null ? cliente.fib_address3_line1 : "";
                    calle3 = cliente[0]["address3_line1"] != null ? cliente[0]["address3_line1"].ToString() : "";
                    //cp = cliente.address1_postalcode != null ? cliente.address1_postalcode : "";
                    cp = cliente[0]["address1_postalcode"] != null ? cliente[0]["address1_postalcode"].ToString() : "";
                    //cp2 = cliente.address2_postalcode != null ? cliente.address2_postalcode : "";
                    cp2 = cliente[0]["address2_postalcode"] != null ? cliente[0]["address2_postalcode"].ToString() : "";
                    //cp3 = cliente.fib_address3_postalcode != null ? cliente.fib_address3_postalcode : "";
                    cp3 = cliente[0]["address3_postalcode"] != null ? cliente[0]["address3_postalcode"].ToString() : "";
                    //nombrecontacto = (cliente.fib_apellidopaterno != null ? cliente.fib_apellidopaterno : "") + " " + (cliente.fib_apellidomaterno != null ? cliente.fib_apellidomaterno : "") + " " + (cliente.address1_primarycontactname != null ? cliente.address1_primarycontactname : "") + " " + (cliente.fib_segundonombre != null ? cliente.fib_segundonombre : "");
                    nombrecontacto = (cliente[0]["fib_apellidopaterno"] != null ? cliente[0]["fib_apellidopaterno"].ToString() : "") + " " + (cliente[0]["fib_apellidomaterno"] != null ? cliente[0]["fib_apellidomaterno"].ToString() : "") + " " + (cliente[0]["address1_primarycontactname"] != null ? cliente[0]["address1_primarycontactname"].ToString() : "") + " " + (cliente[0]["fib_segundonombre"] != null ? cliente[0]["fib_segundonombre"].ToString() : "");
                    //telefono1 = cliente.telephone3 != null ? cliente.telephone3 : "";
                    telefono1 = cliente[0]["telephone3"] != null ? cliente[0]["telephone3"].ToString() : "";
                    //telefono2 = cliente.fax != null ? cliente.fax : "";
                    telefono2 = cliente[0]["fax"] != null ? cliente[0]["fax"].ToString() : "";
                    //email = cliente.emailaddress1 != null ? cliente.emailaddress1 : "";
                    email = cliente[0]["emailaddress1"] != null ? cliente[0]["emailaddress1"].ToString() : "";
                    //impuestos = cliente.fib_impuestoaplicable != null ? cliente.fib_impuestoaplicable.Value == 4 ? "IVAEXENTO" : "IVAGRAL" : "IVAGRAL";
                    impuestos = cliente[0]["fib_impuestoaplicable"] != null ? cliente[0]["fib_impuestoaplicable"].ToString() == "4" ? "IVAEXENTO" : "IVAGRAL" : "IVAGRAL";


                    idcolonia = (new Guid(cliente[0]["fib_coloniaid"].ToString()));
                    //cols.Attributes = new string[] { "fib_name" };
                    atributos = new string[] { "fib_name" };
                    //SdkWs.fib_colonia nomcolonia = (SdkWs.fib_colonia)servicio.Retrieve(SdkWs.EntityName.fib_colonia.ToString(), idcolonia, cols);
                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                    request = new ExecuteFetchRequest { FetchXml = cadFetch };
                    response = (ExecuteFetchResponse)servicio.Execute(request);
                    result = response.FetchXmlResult;
                    List<Hashtable> nomcolonia = this.XmlToMap(result, atributos);

                    colonia = nomcolonia[0]["fib_name"].ToString();
                    colonia = colonia.Substring(colonia.IndexOf("-") + 1, colonia.Length - ((colonia.IndexOf("-")) + 1));

                    if (cliente[0]["fib_coloniaid2"] != null)
                    {
                        idcolonia = (new Guid(cliente[0]["fib_coloniaid2"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        nomcolonia = this.XmlToMap(result, atributos);
                        colonia2 = nomcolonia[0]["fib_name"].ToString();
                        colonia2 = colonia2.Substring(colonia2.IndexOf("-") + 1, colonia2.Length - ((colonia2.IndexOf("-")) + 1));
                    }


                    if (cliente[0]["fib_coloniaid3"] != null)
                    {
                        idcolonia = (new Guid(cliente[0]["fib_coloniaid3"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        nomcolonia = this.XmlToMap(result, atributos);
                        colonia3 = nomcolonia[0]["fib_name"].ToString();
                        colonia3 = colonia3.Substring(colonia3.IndexOf("-") + 1, colonia3.Length - ((colonia3.IndexOf("-")) + 1));
                    }
                    // RGomezS ->
                    if (cliente[0]["fib_formadepagoid"] != null)
                    {
                        idformapago = (new Guid(cliente[0]["fib_formadepagoid"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_formadepago'>" +
                          "          <attribute name='fib_codigo' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_formadepagoid' operator='eq' value='" + idformapago.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        atributos = new string[] { "fib_codigo" };
                        List<Hashtable> nomformapago = this.XmlToMap(result, atributos);
                        formapago = nomformapago[0]["fib_codigo"].ToString();
                    }
                    digitos = cliente[0]["fib_digitos"] != null ? cliente[0]["fib_digitos"].ToString() : "";
                    email2 = cliente[0]["fib_correoelectronicoadicional"] != null ? cliente[0]["fib_correoelectronicoadicional"].ToString() : "";
                    // RGomezS <-

                    if (cliente[0]["fib_usocomprobanteid"] != null)
                    {
                        var usoComprobanteId = new Guid(cliente[0]["fib_usocomprobanteid"].ToString());

                        var hashTable = ObtenCodigoUsoComprobante(servicio, usoComprobanteId);

                        usoComprobanteCodigo = hashTable[0]["fib_codigo"].ToString();
                    }

                    if (cliente[0]["fib_regimenfiscalid"] != null)
                    {
                        var regimenFiscalId = new Guid(cliente[0]["fib_regimenfiscalid"].ToString());

                        var hashTable = ObtenCodigoRegimen(servicio, regimenFiscalId);

                        regimenFiscalCodigo = hashTable[0]["fib_codigo"].ToString();
                    }

                    codigoPostal = cliente[0]["fib_codigopostalcfdi"] != null ? cliente[0]["fib_codigopostalcfdi"].ToString() : string.Empty;
                    razonSocial = cliente[0]["fib_razonsocialcfdi"] != null ? cliente[0]["fib_razonsocialcfdi"].ToString() : string.Empty;
                }
                else
                {
                    atributos = new string[] {"fib_numpersonafisica", "lastname", "fib_apellidomaterno", "firstname", "middlename", "fib_rfc",
                                                "fib_curp","address1_line1","fib_coloniaid","address1_postalcode", 
                                                "fib_address3_line1","fib_coloniaid3","fib_address3_postalcode",
                                                "fib_address4_line1","fib_coloniaid4","fib_address4_postalcode",
                                                "telephone2","mobilephone","emailaddress1","fib_impuestosaplicables","customertypecode",
                                                "fib_formadepagoid","fib_digitos","fib_correoelectronicoadicional","fib_paisdenacimientoid",
                                                "fib_usocomprobanteid", "fib_regimenfiscalid", "fib_codigopostalcfdi", "gendercode"}; // RGomezS

                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                              "      <entity name='contact'>" +
                              "          <attribute name='fib_numpersonafisica' />" +
                              "          <attribute name='lastname' />" +
                              "          <attribute name='fib_apellidomaterno' />" +
                              "          <attribute name='firstname' />" +
                              "          <attribute name='middlename' />" +
                              "          <attribute name='fib_rfc' />" +
                              "          <attribute name='fib_curp' />" +
                              "          <attribute name='address1_line1' />" +
                              "          <attribute name='fib_coloniaid' />" +
                              "          <attribute name='address1_postalcode' />" +
                              "          <attribute name='fib_address3_line1' />" +
                              "          <attribute name='fib_coloniaid3' />" +
                              "          <attribute name='fib_address3_postalcode' />" +
                              "          <attribute name='fib_address4_line1' />" +
                              "          <attribute name='fib_coloniaid4' />" +
                              "          <attribute name='fib_address4_postalcode' />" +
                              "          <attribute name='telephone2' />" +
                              "          <attribute name='mobilephone' />" +
                              "          <attribute name='emailaddress1' />" +
                              "          <attribute name='fib_impuestosaplicables' />" +
                              "          <attribute name='customertypecode' />" +
                              "          <attribute name='fib_formadepagoid' />" +
                              "          <attribute name='fib_digitos' />" +
                              "          <attribute name='fib_correoelectronicoadicional' />" +
                              "          <attribute name='fib_paisdenacimientoid' />" +
                              "          <attribute name='fib_usocomprobanteid' />" +
                              "          <attribute name='fib_regimenfiscalid' />" +
                              "          <attribute name='fib_codigopostalcfdi' />" +
                              "          <attribute name='gendercode' />" +
                              "		     <filter type='and'>" +
                              "              <condition attribute='contactid' operator='eq' value='" + idCliente.ToString() + "' />" +
                              "          </filter>" +
                              "      </entity>" +
                              "  </fetch>";
                    request = new ExecuteFetchRequest { FetchXml = cadFetch };
                    response = (ExecuteFetchResponse)servicio.Execute(request);
                    result = response.FetchXmlResult;
                    
                    List<Hashtable> cliente = this.XmlToMap(result, atributos);


                    //nomcliente = (cliente.lastname != null ? cliente.lastname : "") + " " + (cliente.fib_apellidomaterno != null ? cliente.fib_apellidomaterno : "") + " " + (cliente.firstname != null ? cliente.firstname : "") + " " + (cliente.middlename != null ? cliente.middlename : "");
                    nomcliente = (cliente[0]["lastname"] != null ? cliente[0]["lastname"].ToString() : "") + " " + (cliente[0]["fib_apellidomaterno"] != null ? cliente[0]["fib_apellidomaterno"].ToString() : "") + " " + (cliente[0]["firstname"] != null ? cliente[0]["firstname"].ToString() : "") + " " + (cliente[0]["middlename"] != null ? cliente[0]["middlename"].ToString() : "");
                    //nom1 = cliente.firstname != null ? cliente.firstname : "";
                    nom1 = cliente[0]["firstname"] != null ? cliente[0]["firstname"].ToString() : "";
                    //nom2 = cliente.middlename != null ? cliente.middlename : "";
                    nom2 = cliente[0]["middlename"] != null ? cliente[0]["middlename"].ToString() : "";
                    //apellidop = cliente.lastname != null ? cliente.lastname : "";
                    apellidop = cliente[0]["lastname"] != null ? cliente[0]["lastname"].ToString() : "";
                    //apellidom = cliente.fib_apellidomaterno != null ? cliente.fib_apellidomaterno : "";
                    apellidom = cliente[0]["fib_apellidomaterno"] != null ? cliente[0]["fib_apellidomaterno"].ToString() : "";
                    //codigocliente = cliente.fib_numpersonafisica != null ? cliente.fib_numpersonafisica : "";
                    codigocliente = cliente[0]["fib_numpersonafisica"] != null ? cliente[0]["fib_numpersonafisica"].ToString() : "";
                    //curp = cliente.fib_curp != null ? cliente.fib_curp : "";
                    curp = cliente[0]["fib_curp"] != null ? cliente[0]["fib_curp"].ToString() : "";
                    //rfc = cliente.fib_rfc != null ? cliente.fib_rfc : "";
                    rfc = cliente[0]["fib_rfc"] != null ? cliente[0]["fib_rfc"].ToString() : "";
                    //calle = cliente.address1_line1 != null ? cliente.address1_line1 : "";
                    calle = cliente[0]["address1_line1"] != null ? cliente[0]["address1_line1"].ToString() : "";
                    //calle2 = cliente.fib_address3_line1 != null ? cliente.fib_address3_line1 : "";
                    calle2 = cliente[0]["address3_line1"] != null ? cliente[0]["address3_line1"].ToString() : "";
                    //calle3 = cliente.fib_address4_line1 != null ? cliente.fib_address4_line1 : "";
                    calle3 = cliente[0]["address4_line1"] != null ? cliente[0]["address4_line1"].ToString() : "";
                    //cp = cliente.address1_postalcode != null ? cliente.address1_postalcode : "";
                    cp = cliente[0]["address1_postalcode"] != null ? cliente[0]["address1_postalcode"].ToString() : "";
                    //cp2 = cliente.fib_address3_postalcode != null ? cliente.fib_address3_postalcode : "";
                    cp2 = cliente[0]["address3_postalcode"] != null ? cliente[0]["address3_postalcode"].ToString() : "";
                    //cp3 = cliente.fib_address4_postalcode != null ? cliente.fib_address4_postalcode : "";
                    cp3 = cliente[0]["address4_postalcode"] != null ? cliente[0]["address4_postalcode"].ToString() : "";
                    nombrecontacto = nomcliente;
                    //telefono1 = cliente.telephone2 != null ? cliente.telephone2 : "";
                    telefono1 = cliente[0]["telephone2"] != null ? cliente[0]["telephone2"].ToString() : "";
                    //telefono2 = cliente.mobilephone != null ? cliente.mobilephone : "";
                    telefono2 = cliente[0]["mobilephone"] != null ? cliente[0]["mobilephone"].ToString() : "";
                    //email = cliente.emailaddress1 != null ? cliente.emailaddress1 : "";
                    email = cliente[0]["emailaddress1"] != null ? cliente[0]["emailaddress1"].ToString() : "";
                    //impuestos = cliente.fib_impuestosaplicables != null ? cliente.fib_impuestosaplicables.Value == 4 ? "IVAEXENTO" : "IVAGRAL" : "IVAGRAL";
                    impuestos = cliente[0]["fib_impuestosaplicables"] != null ? cliente[0]["fib_impuestosaplicables"].ToString() == "4" ? "IVAEXENTO" : "IVAGRAL" : "IVAGRAL";

                    ///Tipo de cliente
                    switch (int.Parse(cliente[0]["customertypecode"].ToString()))
                    {
                        case 200000:
                            conClie = "01";
                            break;
                        case 200001:
                            conClie = "02";
                            break;
                        case 200002:
                            conClie = "03";
                            break;
                    }

                    //idcolonia = cliente.fib_coloniaid.Value;
                    idcolonia = (new Guid(cliente[0]["fib_coloniaid"].ToString()));
                    //cols.Attributes = new string[] { "fib_name" };
                    atributos = new string[] { "fib_name" };
                    //SdkWs.fib_colonia nomcolonia = (SdkWs.fib_colonia)servicio.Retrieve(SdkWs.EntityName.fib_colonia.ToString(), idcolonia, cols);
                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                    request = new ExecuteFetchRequest { FetchXml = cadFetch };
                    response = (ExecuteFetchResponse)servicio.Execute(request);
                    result = response.FetchXmlResult;

                    List<Hashtable> nomcolonia = this.XmlToMap(result, atributos);

                    colonia = nomcolonia[0]["fib_name"].ToString();
                    colonia = colonia.Substring(colonia.IndexOf("-") + 1, colonia.Length - ((colonia.IndexOf("-")) + 1));

                    if (cliente[0]["fib_coloniaid3"] != null)
                    {
                        idcolonia = (new Guid(cliente[0]["fib_coloniaid3"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        nomcolonia = this.XmlToMap(result, atributos);
                        colonia2 = nomcolonia[0]["fib_name"].ToString();
                        colonia2 = colonia2.Substring(colonia2.IndexOf("-") + 1, colonia2.Length - ((colonia2.IndexOf("-")) + 1));
                    }

                    if (cliente[0]["fib_coloniaid4"] != null)
                    {
                        idcolonia = (new Guid(cliente[0]["fib_coloniaid4"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_colonia'>" +
                          "          <attribute name='fib_name' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_coloniaid' operator='eq' value='" + idcolonia.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        nomcolonia = this.XmlToMap(result, atributos);
                        colonia3 = nomcolonia[0]["fib_name"].ToString();
                        colonia3 = colonia3.Substring(colonia3.IndexOf("-") + 1, colonia3.Length - ((colonia3.IndexOf("-")) + 1));
                    }
                    // RGomezS ->
                    if (cliente[0]["fib_formadepagoid"] != null)
                    {
                        idformapago = (new Guid(cliente[0]["fib_formadepagoid"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='fib_formadepago'>" +
                          "          <attribute name='fib_codigo' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='fib_formadepagoid' operator='eq' value='" + idformapago.ToString() + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        atributos = new string[] { "fib_codigo" };
                        List<Hashtable> nomformapago = this.XmlToMap(result, atributos);
                        formapago = nomformapago[0]["fib_codigo"].ToString();
                    }
                    digitos = cliente[0]["fib_digitos"] != null ? cliente[0]["fib_digitos"].ToString() : "";
                    email2 = cliente[0]["fib_correoelectronicoadicional"] != null ? cliente[0]["fib_correoelectronicoadicional"].ToString() : "";
                    // RGomezS <-
                    //JCEN 14/05/2015
                    if (cliente[0]["fib_paisdenacimientoid"] != null)
                    {
                        //idformapago = (new Guid(cliente[0]["fib_paisdenacimientoid"].ToString()));
                        cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                          "      <entity name='new_pais'>" +
                          "          <attribute name='new_codigopais' />" +
                          "		     <filter type='and'>" +
                          "              <condition attribute='new_paisid' operator='eq' value='" + (new Guid(cliente[0]["fib_paisdenacimientoid"].ToString())) + "' />" +
                          "          </filter>" +
                          "      </entity>" +
                          "  </fetch>";
                        request = new ExecuteFetchRequest { FetchXml = cadFetch };
                        response = (ExecuteFetchResponse)servicio.Execute(request);
                        result = response.FetchXmlResult;

                        atributos = new string[] { "new_codigopais" };
                        List<Hashtable> codigopais = this.XmlToMap(result, atributos);
                        nacionalidad = codigopais[0]["new_codigopais"].ToString();
                    }
                    ///JCEN
                    

                    if (cliente[0]["fib_usocomprobanteid"] != null)
                    {
                        var usoComprobanteId = new Guid(cliente[0]["fib_usocomprobanteid"].ToString());
                        
                        var hashTable = ObtenCodigoUsoComprobante(servicio, usoComprobanteId);

                        usoComprobanteCodigo = hashTable[0]["fib_codigo"].ToString();
                    }

                    if (cliente[0]["fib_regimenfiscalid"] != null)
                    {
                        var regimenFiscalId = new Guid(cliente[0]["fib_regimenfiscalid"].ToString());

                        var hashTable = ObtenCodigoRegimen(servicio, regimenFiscalId);

                        regimenFiscalCodigo = hashTable[0]["fib_codigo"].ToString();
                    }

                    codigoPostal = cliente[0]["fib_codigopostalcfdi"] != null ? cliente[0]["fib_codigopostalcfdi"].ToString() : string.Empty;

                    if (cliente[0]["gendercode"] != null)
                        genero = cliente[0]["gendercode"].ToString() == "1" ? "M" : "F";
                    
                }

                try
                {
                    /* MCASTROL 07/11/2012 CONEXION A AX 
                      * Aqui se agregará la segunda conexión que delimitará a que compañía de AX se conectará
                      * para hacer el alta del cliente dependiendo del producto que tenga asignado el cliente.
                      */
                    Tuple<InfoConexion, Uri> infoUrl;
                    AxServiceProd axws = new AxServiceProd();
                    InfoConexion info;

                    object[] params1 = { codigocliente, nomcliente, nomcliente, "MXN", "", "", impuestos, rfc, curp, nomdir1,calle, colonia.Trim(), cp, nomdir2,calle2, colonia2.Trim(), cp2, nomdir3, calle3, colonia3.Trim(), cp3, nombrecontacto, telefono1, telefono2, email, conClie,
                                       nom1,nom2,apellidop,apellidom, formapago,digitos,email2,nacionalidad, // RGomezS
                                        usoComprobanteCodigo, regimenFiscalCodigo, razonSocial, codigoPostal, genero }; 

                    StringWriter strWriter = new StringWriter();
                    XmlSerializer serializer = new XmlSerializer(params1.GetType());
                    serializer.Serialize(strWriter, params1);
                    string resultXml = strWriter.ToString();
                    strWriter.Close();

                    List<string> errors = new List<string>();

                    //MCASTROL Este metodo debe de replicar en ambas compañías de AX
                    /*-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.*/
                    infoUrl = this.getInfoConexion(servicio, 1);
                    info = infoUrl.First;
                    axws.Url = infoUrl.Second.ToString();
                    resultado = axws.altaCliente(this.DomainLogonName, resultXml, info);

                    if (!resultado.Contains("Exito"))
                        errors.Add("Compañia " + info.empresa + " :" + resultado);

                    infoUrl = this.getInfoConexion(servicio, 2);
                    info = infoUrl.First;
                    axws.Url = infoUrl.Second.ToString();
                    resultado = axws.altaCliente(this.DomainLogonName, resultXml, info);

                    if (!resultado.Contains("Exito"))
                        errors.Add("Compañia " + info.empresa + " :" + resultado);

                    infoUrl = this.getInfoConexion(servicio, 3);
                    info = infoUrl.First;
                    axws.Url = infoUrl.Second.ToString();
                    resultado = axws.altaCliente(this.DomainLogonName, resultXml, info);

                    if (!resultado.Contains("Exito"))
                        errors.Add("Compañia " + info.empresa + " :" + resultado);

                    if (errors.Any())
                        throw new Exception(string.Join("|", errors));
                    /*-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.*/

                    //resultado = axws.altaCliente(this.DomainLogonName, resultXml, info);


                }
                catch (Exception ex)
                {
                    //Delegar error
                    throw new InvalidPluginExecutionException("Error:" + ex.Message);
                }

                if (resultado.Contains("Error"))
                    throw new InvalidPluginExecutionException(resultado);
            }
            return resultado;
        }

        private void setDomainLogonName(IOrganizationService servicio, IPluginExecutionContext context)
        {
            try
            {
                var callingUser = servicio.Retrieve("systemuser", context.UserId, new ColumnSet("domainname"));
                this.DomainLogonName = callingUser["domainname"].ToString();
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message, ex);
            }

        }

        public List<Hashtable> XmlToMap(string resultXml, string[] atributos)
        {
            try
            {
                XmlDocument documento = new XmlDocument();
                documento.LoadXml(resultXml);
                XmlNodeList resultset = documento.DocumentElement.SelectNodes("result");
                if (resultset == null || resultset.Count == 0)
                {
                    return null;
                }
                List<Hashtable> lista = new List<Hashtable>();
                foreach (XmlNode resultado in resultset)
                {
                    Hashtable registro = new Hashtable();
                    foreach (string atributo in atributos)
                    {
                        XmlNode nodo = resultado.SelectSingleNode(atributo);
                        registro.Add(atributo, (nodo != null) ? nodo.InnerText : null);
                    }
                    lista.Add(registro);
                }
                return lista;
            }
            catch (Exception ex)
            {
                throw new Exception("[BasePlugin.XmlToMap()] Error: " + ex.Message, ex.InnerException);
            }
        }

        private Tuple<InfoConexion, Uri> getInfoConexion(IOrganizationService servicio, int tipoCliente)
        {
            InfoConexion info = new InfoConexion();
            string[] atributos;
            string cadFetch = "";
            string dominio = "";

            //tipoCliente 1 = Filial, 2 = Tercero //Otro: Arrendamiento
            switch(tipoCliente)
            {
                case 1:
                     atributos = new string[] { "fib_fil_usuarioax", "fib_fil_dominiousrax", "fib_fil_companiaax", "fib_fil_servidorax", "fib_fil_urlwebserviceax" };
                     cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='fib_configuracion'>" +
                                  "          <attribute name='fib_fil_usuarioax'/>" +
                                  "          <attribute name='fib_fil_dominiousrax'/>" +
                                  "          <attribute name='fib_fil_companiaax'/>" +
                                  "          <attribute name='fib_fil_servidorax'/>" +
                                  "          <attribute name='fib_fil_urlwebserviceax'/>" +
                                  "      </entity>" +
                                  "  </fetch>";
                     break;
 
                case 2:
                    atributos = new string[] { "fib_ter_usuarioax", "fib_ter_dominiousrax", "fib_ter_companiaax", "fib_ter_servidorax", "fib_ter_urlwebserviceax" };
                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='fib_configuracion'>" +
                                  "          <attribute name='fib_ter_usuarioax'/>" +
                                  "          <attribute name='fib_ter_dominiousrax'/>" +
                                  "          <attribute name='fib_ter_companiaax'/>" +
                                  "          <attribute name='fib_ter_servidorax'/>" +
                                  "          <attribute name='fib_ter_urlwebserviceax'/>" +
                                  "      </entity>" +
                                  "  </fetch>";
                    break;
                default:
                    atributos = new string[] { "fib_arr_usuarioax", "fib_arr_dominiousrax", "fib_arr_companiaax", "fib_arr_servidorax", "fib_arr_urlwebserviceax" };
                    cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='fib_configuracion'>" +
                                  "          <attribute name='fib_arr_usuarioax'/>" +
                                  "          <attribute name='fib_arr_dominiousrax'/>" +
                                  "          <attribute name='fib_arr_companiaax'/>" +
                                  "          <attribute name='fib_arr_servidorax'/>" +
                                  "          <attribute name='fib_arr_urlwebserviceax'/>" +
                                  "      </entity>" +
                                  "  </fetch>";
                    break;
            }

            /*
            if (tipoCliente == 1)
            {
                atributos = new string[] { "fib_fil_usuarioax", "fib_fil_dominiousrax", "fib_fil_companiaax", "fib_fil_servidorax", "fib_fil_urlwebserviceax" };
                cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='fib_configuracion'>" +
                                  "          <attribute name='fib_fil_usuarioax'/>" +
                                  "          <attribute name='fib_fil_dominiousrax'/>" +
                                  "          <attribute name='fib_fil_companiaax'/>" +
                                  "          <attribute name='fib_fil_servidorax'/>" +
                                  "          <attribute name='fib_fil_urlwebserviceax'/>" +
                                  "      </entity>" +
                                  "  </fetch>";
            }
            else
            {
                atributos = new string[] { "fib_ter_usuarioax", "fib_ter_dominiousrax", "fib_ter_companiaax", "fib_ter_servidorax", "fib_ter_urlwebserviceax" };
                cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> " +
                                  "      <entity name='fib_configuracion'>" +
                                  "          <attribute name='fib_ter_usuarioax'/>" +
                                  "          <attribute name='fib_ter_dominiousrax'/>" +
                                  "          <attribute name='fib_ter_companiaax'/>" +
                                  "          <attribute name='fib_ter_servidorax'/>" +
                                  "          <attribute name='fib_ter_urlwebserviceax'/>" +
                                  "      </entity>" +
                                  "  </fetch>";
            }*/
            var request = new ExecuteFetchRequest { FetchXml = cadFetch };
            var response = (ExecuteFetchResponse)servicio.Execute(request);

            String result = response.FetchXmlResult;
            //PConsole.writeLine(result);
            List<Hashtable> configuraciones = this.XmlToMap(result, atributos);
            Uri uri = null;
            if (configuraciones != null)
            {
                string[] arruser = this.DomainLogonName.Split('\\');
                dominio = arruser[0];
                info.usuario = arruser[1];
                //info.dominio = dominio.ToLower() + ".bepensa.local";
                info.dominio = dominio.ToLower().Trim() == "bepensa" ? dominio.ToLower() + ".local" : dominio.ToLower() + ".bepensa.local";

                switch (tipoCliente)
                {
                    case 1:
                        info.empresa = configuraciones[0]["fib_fil_companiaax"].ToString();
                        info.servidor = configuraciones[0]["fib_fil_servidorax"].ToString();
                        uri = new Uri(configuraciones[0]["fib_fil_urlwebserviceax"].ToString());
                        break;
                    case 2:
                        info.empresa = configuraciones[0]["fib_ter_companiaax"].ToString();
                        info.servidor = configuraciones[0]["fib_ter_servidorax"].ToString();
                        uri = new Uri(configuraciones[0]["fib_ter_urlwebserviceax"].ToString());
                        break;
                    default:
                        info.empresa = configuraciones[0]["fib_arr_companiaax"].ToString();
                        info.servidor = configuraciones[0]["fib_arr_servidorax"].ToString();
                        uri = new Uri(configuraciones[0]["fib_arr_urlwebserviceax"].ToString());
                        break;

                }

                /*
                if (tipoCliente == 1)
                {
                    //info.usuario = configuraciones[0]["fib_fil_usuarioax"].ToString();
                    //info.dominio = configuraciones[0]["fib_fil_dominiousrax"].ToString();
                    info.empresa = configuraciones[0]["fib_fil_companiaax"].ToString();
                    info.servidor = configuraciones[0]["fib_fil_servidorax"].ToString();
                    uri = new Uri(configuraciones[0]["fib_fil_urlwebserviceax"].ToString());
                }
                else
                {
                    //info.usuario = configuraciones[0]["fib_ter_usuarioax"].ToString();
                    //info.dominio = configuraciones[0]["fib_ter_dominiousrax"].ToString();
                    info.empresa = configuraciones[0]["fib_ter_companiaax"].ToString();
                    info.servidor = configuraciones[0]["fib_ter_servidorax"].ToString();
                    uri = new Uri(configuraciones[0]["fib_ter_urlwebserviceax"].ToString());
                }*/
            }
            return new Tuple<InfoConexion, Uri>(info, uri);
        }

        private int getTipoProducto(IOrganizationService servicio, Guid LineaCredito)
        {
            string[] atributos;
            string cadFetch = "";
            int tipoProducto = 0;

            //tipoCliente 1 = Filial, 2 = Tercero
            atributos = new string[] { "fib_lineadecreditoid", "fib_esquemaid.fib_tipodeproducto" };
            cadFetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>" +
                            "<entity name='fib_lineadecredito'>" +
                                "<attribute name='fib_lineadecreditoid'/>" +
                                "<order attribute='createdon' descending='false'/>" +
                                "<filter type='and'>" +
                                    "<condition attribute='fib_lineadecreditoid' operator='eq' value='" + LineaCredito.ToString() + "'/>" +
                                "</filter>" +
                                "<link-entity name='fib_disponibleporproducto' from='fib_lineadecreditoid' to='fib_lineadecreditoid'>" +
                                    "<link-entity name='fib_producto' from='fib_productoid' to='fib_productoid'>" +
                                        "<link-entity name='fib_esquema' from='fib_esquemaid' to='fib_esquemaid'>" +
                                            "<attribute name='fib_tipodeproducto' alias='tipodeproducto'/>" +
                                        "</link-entity>" +
                                    "</link-entity>" +
                                "</link-entity>" +
                            "</entity>" +
                        "</fetch>";
            var request = new ExecuteFetchRequest { FetchXml = cadFetch };
            var response = (ExecuteFetchResponse)servicio.Execute(request);

            String result = response.FetchXmlResult;
            
            List<Hashtable> producto = this.XmlToMap(result, atributos);
            if (producto[0]["fib_esquemaid.fib_tipodeproducto"] != null)
            {
                tipoProducto = int.Parse(producto[0]["fib_esquemaid.fib_tipodeproducto"].ToString());
                return tipoProducto;
            }
            else
            {
                throw new Exception("ASIGNE EL TIPO DE PRODUCTO AL ESQUEMA DE LOS PRODUCTOS DE ESTE CLIENTE.");
            }
        }

        private List<Hashtable> ObtenCodigoUsoComprobante(IOrganizationService servicio, Guid usoComprobanteId)
        {
            string[] atributos = new string[1] { "fib_codigo" };
            
            string fetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>       " +
                "<entity name='fib_usocomprobante'>          " +
                "<attribute name='fib_codigo' />\t\t     " +
                "<filter type='and'>              " +
                "<condition attribute='fib_usocomprobanteid' operator='eq' value='" + usoComprobanteId.ToString() + "' />          " +
                "</filter>      " +
                "</entity>  " +
                "</fetch>";

            var request = new ExecuteFetchRequest { FetchXml = fetch };
            var response = (ExecuteFetchResponse)servicio.Execute(request);
            return this.XmlToMap(response.FetchXmlResult, atributos);
        }

        private List<Hashtable> ObtenCodigoRegimen(IOrganizationService servicio, Guid regimenFiscalId)
        {
            string[] atributos = new string[1] { "fib_codigo" };
            
            string fetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>       " +
                "<entity name='fib_regimenfiscal'>          " +
                "<attribute name='fib_codigo' />        " +
                "<filter type='and'>              " +
                "<condition attribute='fib_regimenfiscalid' operator='eq' value='" + regimenFiscalId.ToString() + "' />          " +
                "</filter>      " +
                "</entity>  " +
                "</fetch>";

            var request = new ExecuteFetchRequest { FetchXml = fetch };
            var response = (ExecuteFetchResponse)servicio.Execute(request);

            return this.XmlToMap(response.FetchXmlResult, atributos);
        }
    }
}
