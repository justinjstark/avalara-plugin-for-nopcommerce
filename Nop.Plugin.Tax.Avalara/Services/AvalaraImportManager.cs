using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.ExportImport.Help;
using Nop.Services.Tax;
using OfficeOpenXml;

namespace Nop.Plugin.Tax.Avalara.Services
{
    /// <summary>
    /// Represents Avalara import manager
    /// </summary>
    public partial class AvalaraImportManager
    {
        #region Constants

        /// <summary>
        /// Number of the row with column titles
        /// </summary>
        private const int TITLE_ROW_INDEX = 2;

        /// <summary>
        /// Number of blank rows after which import is ended
        /// </summary>
        private const int BLANK_ROW_NUMBER = 3;

        #endregion

        #region Properties

        /// <summary>
        /// Get the generic attribute name that is used to store tax code of the tax category
        /// </summary>
        public string AvaTaxCodeAttribute { get { return "AvaTaxCode"; } }

        #endregion

        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ITaxCategoryService _taxCategoryService;

        #endregion

        #region Ctor

        public AvalaraImportManager(IGenericAttributeService genericAttributeService, 
            ITaxCategoryService taxCategoryService)
        {
            this._genericAttributeService = genericAttributeService;
            this._taxCategoryService = taxCategoryService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get property manager for the imported file
        /// </summary>
        /// <param name="worksheet">Excel worksheet</param>
        /// <returns>Property manager</returns>
        protected virtual PropertyManager<object> GetPropertyManager(ExcelWorksheet worksheet)
        {
            var properties = new List<PropertyByName<object>>();

            //create properties by column titles
            var columnIndex = 1;
            while (true)
            {
                try
                {
                    //get next title cell
                    var cell = worksheet.Cells[TITLE_ROW_INDEX, columnIndex++];
                    if (cell == null || cell.Value == null || string.IsNullOrEmpty(cell.Value.ToString()))
                        break;

                    //add property
                    var propertyName = GetPropertyNameByColumnTitle(cell.Value.ToString());
                    properties.Add(new PropertyByName<object>(propertyName));
                }
                catch { break; }
            }
            
            return new PropertyManager<object>(properties);
        }

        /// <summary>
        /// Get short property name for the column title
        /// </summary>
        /// <param name="columnTitle">Title of the column</param>
        /// <returns>Property name</returns>
        protected virtual string GetPropertyNameByColumnTitle(string columnTitle)
        {
            columnTitle = columnTitle.Trim();

            if (columnTitle.Equals("AvaTax System Tax Code", StringComparison.InvariantCultureIgnoreCase))
                return "SystemName";

            if (columnTitle.Equals("AvaTax System Tax Code Description", StringComparison.InvariantCultureIgnoreCase))
                return "Name";

            return columnTitle;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Import AvaTax tax codes from Excel file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual int ImportTaxCodesFromXlsx(Stream stream)
        {
            using (var xlPackage = new ExcelPackage(stream))
            {
                //get property manager
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                var manager = GetPropertyManager(worksheet);

                //get existing tax categories
                var existingTaxCategories = _taxCategoryService.GetAllTaxCategories();

                var savedTaxCodesNumber = 0;
                var blankRows = 0;
                var rowIndex = TITLE_ROW_INDEX + 1;
                while (true)
                {
                    //read values from cells
                    manager.ReadFromXlsx(worksheet, rowIndex++);

                    //check whether all cells in the current row are empty
                    if (manager.GetProperties.All(property => string.IsNullOrEmpty(property.StringValue)))
                        blankRows++;
                    else
                        blankRows = 0;

                    //file is ended
                    if (blankRows >= BLANK_ROW_NUMBER)
                        break;

                    //get values of the name and the system name 
                    var taxCodeName = manager.GetProperty("Name").StringValue;
                    var taxCodeSystemName = manager.GetProperty("SystemName").StringValue;
                    if (string.IsNullOrEmpty(taxCodeSystemName))
                        continue;

                    //try to get tax category by the name
                    var taxCategory = existingTaxCategories.FirstOrDefault(category => category.Name.Equals(taxCodeName, StringComparison.InvariantCultureIgnoreCase));

                    //create the new one if not exist
                    if (taxCategory == null)
                    {
                        taxCategory = new TaxCategory
                        {
                            Name = taxCodeName
                        };
                        _taxCategoryService.InsertTaxCategory(taxCategory);
                    }

                    //save AvaTax system tax code as attribute of the tax category
                    _genericAttributeService.SaveAttribute(taxCategory, AvaTaxCodeAttribute, taxCodeSystemName);
                    savedTaxCodesNumber++;
                }

                return savedTaxCodesNumber;
            }
        }

        #endregion
    }
}