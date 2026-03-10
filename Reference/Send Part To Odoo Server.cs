/* *******************************************************************************************
Method Name:            ISS Send Part to Odoo Server            ooooo  .oooooo..o  .oooooo..o 
Created By:				Steven Spradlin                         `888' d8P'    `Y8 d8P'    `Y8 
Company:                Intrepid Software Solutions              888  Y88bo.      Y88bo.      
Creation Date:          2025-04-29                               888   `"Y8888o.   `"Y8888o.  
                                                                 888       `"Y88b      `"Y88b 
                                                                 888  oo     .d8P oo     .d8P 
Description:                                                    o888o 8""88888P'  8""88888P'  

    Sends a Part and all child BOMs in Aras to Odoo as a Product.
	
Revisions:
	Rev Date		Modified By		    Description
	2025-Jan-05		Steven Spradlin	    Initial creation
	2025-04-29		Ernesto Ruiz	    Updated to Odoo 18
********************************************************************************************** */
//WARNING: DO NOT FORGET TO DISABLE THIS IN A PRODUCTION ENVIRONMENT!!!
//if (System.Diagnostics.Debugger.Launch()) System.Diagnostics.Debugger.Break();  //enable/disable the debugger as required
  
string methodName = "ISS Send Part to Odoo Server";
Innovator inn = this.getInnovator();

string partId = this.getProperty("partId","");
string tempWorkingFolder = string.Empty;

Aras.Server.Security.Identity plmIdentity = Aras.Server.Security.Identity.GetByName("Aras PLM");
try
{
    using (CCO.Permissions.GrantIdentity(plmIdentity))
    {
        Item worldPreference = inn.getItemById("ISS_Aras_Odoo_Connector", "E1E7DFC68D4443BD894291DE1578EF29");
    
        string odooURL = worldPreference.getProperty("iss_odoo_url", "");
        string odooDatabase = worldPreference.getProperty("iss_odoo_database", "");
        string odooApiKey = worldPreference.getProperty("iss_odoo_api_key", "");
        string odooWorkingFolder = worldPreference.getProperty("iss_odoo_temp_folder", "");
        int odooUserId = Convert.ToInt32(worldPreference.getProperty("iss_odoo_user_id", "0"));
        
        if (String.IsNullOrEmpty(odooURL) || String.IsNullOrEmpty(odooDatabase) || String.IsNullOrEmpty(odooApiKey) || odooUserId == 0 || String.IsNullOrEmpty(odooWorkingFolder))
        {
          throw new Exception("World Preference must contain values.");
        }
        
        string amlGetPartBoms = @"<AML>
        							  <Item type='Part' action='GetItemRepeatConfig' id='{0}'>
        							   <Relationships>
        								<Item type='Part BOM' select='related_id,quantity' repeatProp='related_id' repeatTimes='10'/>
        							   </Relationships>
        							 </Item>
        							</AML>";
        Item parentPart = inn.applyAML(String.Format(amlGetPartBoms, partId));
        
        Item parts = parentPart.getItemsByXPath("//Item[@type='Part']");
        
        // Dictionary to store each parent with its child list
        Dictionary<string, List<(string childItemNumber, double qty)>> bomStructure = new();
        
        // Loop to add full Part Heirarchy to Odoo
        for (int i = 0; i < parts.getItemCount(); i++)
        {
        	Item part = parts.getItemByIndex(i);
        	
            string itemNumber = part.getProperty("item_number", "");
            string itemName = part.getProperty("name", "");
            string itemDescription = part.getProperty("description", "");
            string itemClassification = part.getProperty("classification", "");
            string itemMakeBuy = part.getProperty("make_buy", "");
            string itemThumbnailFileID = part.getProperty("thumbnail", "");
            string thumbnailFileName = "";
            
            // Get child BOMs
            var bomComponents = new List<(string, double)>();
            
            Item bomRels = part.getRelationships("Part BOM");
            for (int j = 0; j < bomRels.getItemCount(); j++)
            {
                Item bom = bomRels.getItemByIndex(j);
                Item child = bom.getRelatedItem();
                if (child != null)
                {
                    string childNumber = child.getProperty("item_number");
                    double qty = double.Parse(bom.getProperty("quantity", "1"));
                    bomComponents.Add((childNumber, qty));
                }
            }
        
            if (bomComponents.Count > 0)
            {
                bomStructure[itemNumber] = bomComponents;
            }
            
            if (!string.IsNullOrEmpty(itemThumbnailFileID))
            {
                string[] splitString = itemThumbnailFileID.Split('=');
                
                Item thumbnailFile = inn.getItemById("File", splitString[1]);
                
                //create temp directory to work from
                string tempString = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                tempWorkingFolder = Path.Combine(odooWorkingFolder, thumbnailFile.getID());
                Directory.CreateDirectory(tempWorkingFolder);
                
                //get file from vault and put in temp path
                thumbnailFileName = thumbnailFile.getProperty("filename", "");
                Item checkout = thumbnailFile.checkout(tempWorkingFolder);
                if(checkout.isError())
                {
                    return checkout;
                }
            }
            
            // Call the create method on product.template
            if (!string.IsNullOrEmpty(itemThumbnailFileID))
            {
                int newProductId = CreateOdooProduct(odooURL, odooDatabase, odooUserId, odooApiKey, itemNumber, itemName, itemDescription, "product", tempWorkingFolder + '\\' + thumbnailFileName);
            }
            else
            {
                int newProductId = CreateOdooProduct(odooURL, odooDatabase, odooUserId, odooApiKey, itemNumber, itemName, itemDescription, "product", null);
            }
        }
        
        // Loop to create BOM Relationships in Odoo
        foreach (var kvp in bomStructure)
        {
            string parentItemNumber = kvp.Key;
            List<(string childItemNumber, double qty)> children = kvp.Value;
        
            try
            {
                CreateOdooBOM(odooURL, odooDatabase, odooUserId, odooApiKey, parentItemNumber, children);
            }
            catch (Exception ex)
            {
                return inn.newError($"Failed to create BOM for '{parentItemNumber}': {ex.Message}");
            }
        }

    }
    return inn.newResult("OK");;
}
catch(Exception ex)
{
    return inn.newError($"{methodName}: {ex.Message}");
}  
finally
{
    if (!String.IsNullOrEmpty(tempWorkingFolder))
    {
        Directory.Delete(tempWorkingFolder, true);
    }
}}


private static int CreateOdooProduct(string url, string dbName, int userId, string apiKey, string partNumber, string partName, string description, string partType, string imagePath)
{
    string base64Image = null;
    if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        base64Image = Convert.ToBase64String(imageBytes);
    }

    var payload = new
    {
        jsonrpc = "2.0",
        method = "call",
        @params = new
        {
            service = "object",
            method = "execute_kw",
            args = new object[]
            {
                dbName,
                userId,
                apiKey,
                "product.template",
                "create",
                new object[]
                {
                    new
                    {
                        default_code = partNumber,
                        name = partName,
                        description_sale = description,
                        image_1920 = base64Image
                    }
                }
            }
        },
        id = 2
    };

    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

    using (var client = new System.Net.Http.HttpClient())
    {
        var content = new System.Net.Http.StringContent(jsonData, Encoding.UTF8, "application/json");
        try
        {
            var response = client.PostAsync(url, content).Result;
            string responseText = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Failed to create product in Odoo. Status: {response.StatusCode}, Error: {responseText}");
            }

            dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);
            if (parsed.result != null)
            {
                return Convert.ToInt32(parsed.result);
            }
            else
            {
                throw new Exception("Odoo returned no product ID after creation.");
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error during product creation in Odoo: " + ex.Message);
        }
    }
}


private static void CreateOdooBOM(string url, string dbName, int userId, string apiKey, string parentItemNumber, List<(string childItemNumber, double qty)> children)
{
    int templateId = LookupProductTemplateId(url, dbName, userId, apiKey, parentItemNumber);
    int uomId = GetProductUoMId(url, dbName, userId, apiKey, templateId);

    var bomLines = new List<object>();
    foreach (var (childCode, qty) in children)
    {
        int productId = LookupProductId(url, dbName, userId, apiKey, childCode);

        bomLines.Add(new object[]
        {
            0, 0, new
            {
                product_id = productId,
                product_qty = qty
            }
        });
    }

    var bomPayload = new
    {
        jsonrpc = "2.0",
        method = "call",
        @params = new
        {
            service = "object",
            method = "execute_kw",
            args = new object[]
            {
                dbName,
                userId,
                apiKey,
                "mrp.bom",
                "create",
                new object[]
                {
                    new
                    {
                        product_tmpl_id = templateId,
                        product_id = false,
                        type = "normal",
                        product_qty = 1.0,
                        product_uom_id = uomId,
                        bom_line_ids = bomLines
                    }
                }
            }
        },
        id = 3
    };

    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(bomPayload);

    using (var client = new System.Net.Http.HttpClient())
    {
        var content = new System.Net.Http.StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = client.PostAsync(url, content).Result;
        string responseText = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create BOM for '{parentItemNumber}'. HTTP {response.StatusCode}: {responseText}");
        }

        dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);
        if (parsed.result == null)
        {
            throw new Exception($"Odoo returned no result when creating BOM for '{parentItemNumber}'.");
        }

        int bomId = Convert.ToInt32(parsed.result);
    }
}


private static int LookupProductTemplateId(string url, string dbName, int userId, string apiKey, string defaultCode)
{
    var payload = new
    {
        jsonrpc = "2.0",
        method = "call",
        @params = new
        {
            service = "object",
            method = "execute_kw",
            args = new object[]
            {
                dbName,
                userId,
                apiKey,
                "product.template",
                "search_read",
                new object[]
                {
                    new List<object> { new List<object> { "default_code", "=", defaultCode } }
                },
                new { fields = new[] { "id" }, limit = 1 }
            }
        },
        id = 10
    };

    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

    using (var client = new System.Net.Http.HttpClient())
    {
        var content = new System.Net.Http.StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = client.PostAsync(url, content).Result;

        if (!response.IsSuccessStatusCode)
        {
            string err = response.Content.ReadAsStringAsync().Result;
            throw new Exception($"Failed to lookup product.template for '{defaultCode}': {err}");
        }

        string result = response.Content.ReadAsStringAsync().Result;
        dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

        if (parsed.result.Count == 0)
        {
            throw new Exception($"Product template with default_code '{defaultCode}' not found.");
        }

        return (int)parsed.result[0].id;
    }
}


private static int LookupProductId(string url, string dbName, int userId, string apiKey, string defaultCode)
{
    var payload = new
    {
        jsonrpc = "2.0",
        method = "call",
        @params = new
        {
            service = "object",
            method = "execute_kw",
            args = new object[]
            {
                dbName,
                userId,
                apiKey,
                "product.product",
                "search_read",
                new object[]
                {
                    new List<object> { new List<object> { "default_code", "=", defaultCode } }
                },
                new { fields = new[] { "id" }, limit = 1 }
            }
        },
        id = 11
    };

    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

    using (var client = new System.Net.Http.HttpClient())
    {
        var content = new System.Net.Http.StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = client.PostAsync(url, content).Result;

        if (!response.IsSuccessStatusCode)
        {
            string err = response.Content.ReadAsStringAsync().Result;
            throw new Exception($"Failed to lookup product.product for '{defaultCode}': {err}");
        }

        string result = response.Content.ReadAsStringAsync().Result;
        dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

        if (parsed.result.Count == 0)
        {
            throw new Exception($"Product variant with default_code '{defaultCode}' not found.");
        }

        return (int)parsed.result[0].id;
    }
}


private static int GetProductUoMId(string url, string dbName, int userId, string apiKey, int templateId)
{
    var payload = new
    {
        jsonrpc = "2.0",
        method = "call",
        @params = new
        {
            service = "object",
            method = "execute_kw",
            args = new object[]
            {
                dbName,
                userId,
                apiKey,
                "product.template",
                "read",
                new object[] { new int[] { templateId } },
                new { fields = new[] { "uom_id" } }
            }
        },
        id = 12
    };

    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

    using (var client = new System.Net.Http.HttpClient())
    {
        var content = new System.Net.Http.StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = client.PostAsync(url, content).Result;
        string responseText = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to look up UoM: " + responseText);
        }

        dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);
        if (parsed.result == null || parsed.result.Count == 0)
        {
            throw new Exception("UoM lookup returned no result.");
        }

        return (int)parsed.result[0].uom_id[0]; 
    }
}


private void end_of_method_()
{