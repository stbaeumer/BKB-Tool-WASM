namespace BKBToolClient.Models;

public class FunctionConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Unterstützt Markdown/HTML
    public List<RequiredFile> RequiredFiles { get; set; } = new();
    public List<RequiredFile> OptionalFiles { get; set; } = new();
    public List<InputField> InputFields { get; set; } = new();
    public List<ProcessingStep> ProcessingSteps { get; set; } = new();
    // Optional hint text shown in the UI next to the function name
    public string FunctionHint { get; set; } = string.Empty;
    public string SubmitButtonLabel { get; set; } = "Verarbeiten";
    public string SuccessMessage { get; set; } = "Verarbeitung erfolgreich abgeschlossen.";
    public List<OutputFileConfig> OutputFiles { get; set; } = new();
    // Optional list of test files (relative or absolute URLs) for this function
    public List<string> TestFiles { get; set; } = new();
}

public class RequiredFile
{
    public string Name { get; set; } = string.Empty;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string FileKey { get; set; } = string.Empty;
    // Optional example filename that helps the user identify the expected upload
    public string ExampleFileName { get; set; } = string.Empty;
    public string SourceHint { get; set; } = string.Empty; // Mehrzeilig möglich
    public bool IsOptional { get; set; }
    public bool IsRequired { get; set; } = true; // maps to JSON 'isRequired'
    public string? MatchingRegex { get; set; } // optional regex to match filenames (e.g. ".*SchuelerBasisdaten.*\\.dat$")

    // New optional parsing hints (from functions.json)
    public string Delimiter { get; set; } = "\t"; // default tab
    public string Encoding { get; set; } = "utf-8";
    public string Quote { get; set; } = string.Empty; // e.g. ' or " or empty

    // Indicate whether this file contains a header line
    public bool HasHeader { get; set; } = true;

    // Runtime-Properties (nicht in JSON)
    public bool IsUploaded { get; set; }
    public string? FileName { get; set; }
    public byte[]? Content { get; set; }
    public int LineCount { get; set; }
    public int ColumnCount { get; set; }

    // Displayable last modified timestamp (e.g. "12.12.2024, 11:40")
    public string? LastModifiedDisplay { get; set; }
}

public class InputField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public List<SelectOption> Options { get; set; } = new();
    public string ValidationRegex { get; set; } = string.Empty;
    public string ValidationErrorMessage { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    
    // Runtime-Property (nicht in JSON)
    public string Value { get; set; } = string.Empty;
}

public class SelectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ProcessingStep
{
    public string FunctionName { get; set; } = string.Empty; // z.B. "SetzeMailadressen"
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class OutputFileConfig
{
    public string FileKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty; // Kann Platzhalter enthalten: {originalName}
    public string Hint { get; set; } = string.Empty;
    public string ProcessingHint { get; set; } = string.Empty; // Hinweise zur Weiterverarbeitung
}

public class ProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageHtml { get; set; }
    public List<OutputFile> OutputFiles { get; set; } = new();
}

public class OutputFile
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string? Hint { get; set; }
    public string? ProcessingHint { get; set; }
    // Metadaten zur Anzeige in der UI
    public int LineCount { get; set; } = 0; // Zeilenzahl (nur für CSV)
    public long FileSize { get; set; } = 0; // Dateigröße in Bytes

    // Formatierte Anzeigen
    public string FormattedSize => FileSize >= 1024 * 1024 ? $"{FileSize / (1024 * 1024)} MB" : FileSize >= 1024 ? $"{FileSize / 1024} KB" : $"{FileSize} B";
}
