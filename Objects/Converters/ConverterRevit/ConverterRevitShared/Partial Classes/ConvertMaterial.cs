﻿using Autodesk.Revit.DB.Structure;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.Visual;
using Objects.Other.Revit;
using DB = Autodesk.Revit.DB;
using System.Diagnostics;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Objects.Other.Material MaterialToSpeckle(DB.Material revitmaterial)
    {

      var RenderAssest = GetAppearanceAssetProperties(revitmaterial);

      var speckleMaterial = new Objects.Other.Revit.RevitMaterial(revitmaterial.Name, revitmaterial.MaterialCategory, revitmaterial.MaterialClass, revitmaterial.Shininess,
          revitmaterial.Smoothness, revitmaterial.Transparency, RenderAssest);

      GetAllRevitParamsAndIds(speckleMaterial, revitmaterial);

      Report.Log($"Converted Material{revitmaterial.Id}");
      return speckleMaterial;
    }



    private Objects.Other.Material ConvertAndCacheMaterial(DB.ElementId id, DB.Document doc)
    {
      var material = doc.GetElement(id) as DB.Material;

      if (material == null) return null;
      if (!Materials.ContainsKey(material.Name))
      {
        Materials[material.Name] = MaterialToSpeckle(material);
      }
      return Materials[material.Name] as Objects.Other.Material;
    }

    private void SetProprtyValue(AssetProperty property, RenderingAssetProperty asstProp)
    {
  
      if (asstProp.Type == "String")
      {
        AssetPropertyString aps = property as AssetPropertyString;
        if (aps.IsEditable())
        {
          aps.Value = asstProp.Value;
        }
      }
      else if (asstProp.Type == "Boolean")
      {
        AssetPropertyBoolean apb = property as AssetPropertyBoolean;
        if (apb.IsEditable())
          apb.Value = asstProp.Value.ToLower() == "true";
      }
      else if (asstProp.Type == "Double1")
      {
        AssetPropertyDouble apd1 = property as AssetPropertyDouble;
        if (apd1.IsEditable())
          apd1.Value = Double.Parse(asstProp.Value);
      }
      else if (asstProp.Type == "Double2")
      {
        AssetPropertyDoubleArray2d apd2 = property as AssetPropertyDoubleArray2d;
        {
        }
      }
      else if (asstProp.Type == "Double4")
      {
        AssetPropertyDoubleArray4d apd4 = property as AssetPropertyDoubleArray4d;
      }
      else if (asstProp.Type == "Integer")
      {
        AssetPropertyInteger api = property as AssetPropertyInteger;
        if (api.IsEditable())
          api.Value = int.Parse(asstProp.Value);
      }

      foreach (var subPram in asstProp.ConnectedProperties)
      {
        var connectedProp = property.GetSingleConnectedAsset().FindByName(subPram.Name);

        SetProprtyValue(connectedProp, subPram);
      }
    }


    public ApplicationObject MaterialToNative(Other.Material instance)
    {
      var docObj = GetExistingElementByApplicationId(instance.applicationId);
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };
      var RevitMaterial = instance as RevitMaterial;
      var allMaterials = new DB.FilteredElementCollector(Doc).OfClass(typeof(DB.Material)).Cast<DB.Material>().ToList();
      var selectedMaterial = allMaterials.FirstOrDefault(x => x.Name == instance.name);
      //Material is already found, so we will update it 
      if (selectedMaterial != null)
      {
        selectedMaterial.Shininess = RevitMaterial.shininess;
        selectedMaterial.Smoothness = RevitMaterial.smoothness;
        selectedMaterial.Transparency = RevitMaterial.transparency;
        selectedMaterial.MaterialClass = RevitMaterial.materialClass;
        selectedMaterial.MaterialCategory = selectedMaterial.MaterialCategory;


        var AssestId = selectedMaterial.AppearanceAssetId;

        DB.AppearanceAssetElement aa = Doc.GetElement(AssestId) as DB.AppearanceAssetElement;
        var renderingAssets = aa.GetRenderingAsset();
        if (aa.Name == RevitMaterial.AppearanceAssetElement.AssestName)
        {
          using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(Doc))
          {
            Asset editableAsset = editScope.Start(aa.Id);
            foreach (var asstProp in RevitMaterial.AppearanceAssetElement.RenderingAssest.AssestProperties)
            {
              try
              {
                var property = editableAsset.FindByName(asstProp.Name);
                SetProprtyValue(property, asstProp);
              }
              catch (Exception ex)
              {
              }
            }


            editScope.Commit(true);
          }
        }

        appObj.Status = ApplicationObject.State.Updated;
      }
      else
      {
        // the material is not found and the receiving mode is to create a one 
        if (ReceiveMode == ReceiveMode.Create)
        {
          //to do later
        }
      }


      return appObj;
    }
    private RevitAppearanceAssetElement GetAppearanceAssetProperties(DB.Material material)
    {
      RevitAppearanceAssetElement appearanceAssetElement = new RevitAppearanceAssetElement();


      DB.ElementId appearanceId = material.AppearanceAssetId;
      DB.AppearanceAssetElement appearanceElem =
        Doc.GetElement(appearanceId) as DB.AppearanceAssetElement;
      Asset theAsset = appearanceElem.GetRenderingAsset();
      appearanceAssetElement.AssestName = appearanceElem.Name;
      appearanceAssetElement.RenderingAssest = new RenderingAssest();
      appearanceAssetElement.RenderingAssest.Name = theAsset.Name;
      appearanceAssetElement.RenderingAssest.AssestProperties = ExtractAssestProperties(theAsset);

      return appearanceAssetElement;
    }


    private List<RenderingAssetProperty> ExtractAssestProperties(Asset theAsset, string subFix = "")
    {
      List<RenderingAssetProperty> renderingAssetProperties = new List<RenderingAssetProperty>();

      List<AssetProperty> assets = new List<AssetProperty>();
      for (int idx = 0; idx < theAsset.Size; idx++)
      {
        AssetProperty ap = theAsset[idx];
        assets.Add(ap);
      }

      // order the properties!
      assets = assets.OrderBy(ap => ap.Name).ToList();
      for (int idx = 0; idx < assets.Count; idx++)
      {
        RenderingAssetProperty rap = new RenderingAssetProperty();
        AssetProperty ap = assets[idx];
        rap = GetAssestPropertyData(subFix, ap, idx);
        var connectedAssestes = ap.GetSingleConnectedAsset();
        if (connectedAssestes != null && connectedAssestes.Size > 0)
        {
          for (int i = 0; i < connectedAssestes.Size; i++)
          {
            rap.ConnectedProperties.Add(GetAssestPropertyData("\t" + "[" + ap.Name + "] ", connectedAssestes.Get(i),
              i));
          }
        }

        renderingAssetProperties.Add(rap);
      }

      return renderingAssetProperties;
    }


    private RenderingAssetProperty GetAssestPropertyData(string subFix, AssetProperty ap, int idx)
    {
      Type type = ap.GetType();
      object apVal = null;
      try
      {
        // using .net reflection to get the value
        var prop = type.GetProperty("Value");
        if (prop != null &&
            prop.GetIndexParameters().Length == 0)
        {
          apVal = prop.GetValue(ap);
        }
        else
        {
          apVal = "<No Value Property>";
        }
      }
      catch (Exception ex)
      {
        apVal = ex.GetType().Name + "-" + ex.Message;
      }

      if (apVal is DB.DoubleArray)
      {
        var doubles = apVal as DB.DoubleArray;
        apVal = doubles.Cast<double>().Aggregate("", (s, d) => s + Math.Round(d, 5) + ",");
      }

      Debug.WriteLine(subFix + idx + " : [" + ap.Type + "] " + ap.Name + " = " + apVal);
      return new RenderingAssetProperty() { Name = ap.Name, Type = ap.Type.ToString(), Value = apVal.ToString() };
    }

  }

}
