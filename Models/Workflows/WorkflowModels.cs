namespace AICodeReviewer.Models.Workflows;

public class WorkflowConfiguration
{
    public string WorkflowName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public WorkflowTriggers Triggers { get; set; } = new();
    public Workflow Workflow { get; set; } = new();
    public Dictionary<string, string> Templates { get; set; } = new();
}

public class WorkflowTriggers
{
    public List<string> Events { get; set; } = new();
}

public class Workflow
{
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Plugin { get; set; } = "";
    public string Function { get; set; } = "";
    public bool Required { get; set; } = true;
    public List<string> DependsOn { get; set; } = new();
    public Dictionary<string, object> Conditions { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WorkflowContext
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
    public string TriggerEvent { get; set; } = "";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<WorkflowStepResult> Results { get; set; } = new();
}

public class WorkflowStepResult
{
    public string StepId { get; set; } = "";
    public string Status { get; set; } = ""; // Success, Failed, Skipped
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public object? Result { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
