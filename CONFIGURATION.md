# AI Code Reviewer Configuration Guide

## Temperature Settings for Azure OpenAI

The `Temperature` setting in `appsettings.json` controls the randomness/creativity of AI responses.

### Temperature Values:

| Value | Behavior                 | Use Case                       | Consistency     |
| ----- | ------------------------ | ------------------------------ | --------------- |
| `0.0` | Completely deterministic | Testing, debugging             | 100% consistent |
| `0.1` | Very low randomness      | **Code reviews (recommended)** | 95%+ consistent |
| `0.2` | Low randomness           | Production code reviews        | 90%+ consistent |
| `0.3` | Slightly creative        | Documentation generation       | 85% consistent  |
| `0.7` | Moderately creative      | Creative writing               | 60% consistent  |
| `1.0` | Highly creative          | Brainstorming                  | 30% consistent  |

### Recommended Settings by Environment:

```json
// Development (appsettings.Development.json)
{
  "AzureOpenAI": {
    "Temperature": 0.1  // Very consistent for testing
  }
}

// Production (appsettings.Production.json)
{
  "AzureOpenAI": {
    "Temperature": 0.2  // Slightly more variation for production
  }
}
```

### Why Low Temperature for Code Reviews?

1. **Consistency**: Same code should produce same review results
2. **Reliability**: Deterministic findings for CI/CD pipelines
3. **Debugging**: Easier to troubleshoot when results are predictable
4. **Fairness**: All developers get consistent review standards

### Configuration Override Hierarchy:

1. `appsettings.Production.json` (production)
2. `appsettings.Development.json` (development)
3. `appsettings.json` (base configuration)
4. Environment variables (highest priority)

### Testing Different Temperatures:

```bash
# Test with different temperatures
curl -X POST "https://localhost:7001/api/pullrequests/review/1"

# Change appsettings.Development.json temperature and restart
# Compare results for consistency
```

### Best Practices:

- **Development**: Use 0.1 for consistent debugging
- **Production**: Use 0.1-0.2 for reliable reviews
- **Testing**: Use 0.0 for completely deterministic results
- **Never use**: 0.7+ for code reviews (too inconsistent)
