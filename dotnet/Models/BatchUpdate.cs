using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class BatchUpdate
    {
        [JsonProperty("requests")]
        public Request[] Requests { get; set; }
    }

    public class Request
    {
        [JsonProperty("repeatCell", NullValueHandling = NullValueHandling.Ignore)]
        public RepeatCell RepeatCell { get; set; }

        [JsonProperty("updateSheetProperties", NullValueHandling = NullValueHandling.Ignore)]
        public UpdateSheetProperties UpdateSheetProperties { get; set; }

        [JsonProperty("setDataValidation", NullValueHandling = NullValueHandling.Ignore)]
        public SetDataValidation SetDataValidation { get; set; }

        [JsonProperty("autoResizeDimensions", NullValueHandling = NullValueHandling.Ignore)]
        public AutoResizeDimensions AutoResizeDimensions { get; set; }
    }

    public class AutoResizeDimensions
    {
        [JsonProperty("dimensions")]
        public Dimensions Dimensions { get; set; }
    }

    public class Dimensions
    {
        [JsonProperty("sheetId")]
        public long SheetId { get; set; }

        [JsonProperty("dimension")]
        public string Dimension { get; set; }

        [JsonProperty("startIndex")]
        public long StartIndex { get; set; }

        [JsonProperty("endIndex")]
        public long EndIndex { get; set; }
    }

    public class SetDataValidation
    {
        [JsonProperty("range")]
        public BatchUpdateRange Range { get; set; }

        [JsonProperty("rule")]
        public Rule Rule { get; set; }
    }

    public class Rule
    {
        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("inputMessage")]
        public string InputMessage { get; set; }

        [JsonProperty("strict")]
        public bool Strict { get; set; }
    }

    public class RepeatCell
    {
        [JsonProperty("range")]
        public BatchUpdateRange Range { get; set; }

        [JsonProperty("cell")]
        public Cell Cell { get; set; }

        [JsonProperty("fields")]
        public string Fields { get; set; }
    }

    public class Cell
    {
        [JsonProperty("userEnteredFormat")]
        public UserEnteredFormat UserEnteredFormat { get; set; }
    }

    public class UserEnteredFormat
    {
        [JsonProperty("backgroundColor")]
        public GroundColor BackgroundColor { get; set; }

        [JsonProperty("horizontalAlignment")]
        public string HorizontalAlignment { get; set; }

        [JsonProperty("textFormat")]
        public BatchUpdateTextFormat TextFormat { get; set; }
    }

    public class GroundColor
    {
        [JsonProperty("red")]
        public double Red { get; set; }

        [JsonProperty("green")]
        public double Green { get; set; }

        [JsonProperty("blue")]
        public double Blue { get; set; }
    }

    public class BatchUpdateTextFormat
    {
        [JsonProperty("foregroundColor")]
        public GroundColor ForegroundColor { get; set; }

        [JsonProperty("fontSize")]
        public long FontSize { get; set; }

        [JsonProperty("bold")]
        public bool Bold { get; set; }
    }

    public class BatchUpdateRange
    {
        [JsonProperty("sheetId")]
        public long SheetId { get; set; }

        [JsonProperty("startRowIndex")]
        public long StartRowIndex { get; set; }

        [JsonProperty("endRowIndex")]
        public long EndRowIndex { get; set; }

        [JsonProperty("startColumnIndex")]
        public long? StartColumnIndex { get; set; }

        [JsonProperty("endColumnIndex")]
        public long? EndColumnIndex { get; set; }
    }

    public class UpdateSheetProperties
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }

        [JsonProperty("fields")]
        public string Fields { get; set; }
    }

    public class Properties
    {
        [JsonProperty("sheetId")]
        public long SheetId { get; set; }

        [JsonProperty("gridProperties")]
        public BatchUpdateGridProperties GridProperties { get; set; }
    }

    public class BatchUpdateGridProperties
    {
        [JsonProperty("frozenRowCount")]
        public long FrozenRowCount { get; set; }
    }
}
