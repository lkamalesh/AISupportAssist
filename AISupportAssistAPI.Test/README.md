# AISupportAssist API - Unit Tests

This project contains comprehensive unit tests for the AISupportAssist.API project using xUnit, Moq, and FluentAssertions.

## Test Coverage

### FaqServiceTests (18 tests)
Tests for the FAQ service that manages FAQ CRUD operations:

- **GetByIdAsync Tests**
  - Returns FAQ when it exists
  - Returns null when FAQ doesn't exist

- **GetAllAsync Tests**
  - Returns all FAQs when they exist
  - Returns empty collection when no FAQs exist
  - Verifies NoTracking is used (entities are not tracked)

- **AddAsync Tests**
  - Adds valid FAQ to database
  - Handles multiple FAQs addition
  - Supports long questions

- **UpdateAsync Tests**
  - Updates existing FAQ successfully
  - Throws concurrency exception for non-existent FAQ
  - Supports partial updates

- **DeleteAsync Tests**
  - Deletes existing FAQ successfully
  - Handles non-existent FAQ without errors
  - Only removes specified FAQ when multiple exist

### GroqServiceTests (12 tests)
Tests for the AI service integration with Groq API:

- **Successful Response Tests**
  - Returns answer content from API
  - Handles empty choices array
  - Returns first choice when multiple choices exist
  - Handles null responses

- **Error Handling Tests**
  - Throws exception on HTTP 400 Bad Request
  - Throws exception on HTTP 500 Internal Server Error
  - Throws exception on HTTP 401 Unauthorized
  - Handles timeouts with TaskCanceledException

- **Request Validation Tests**
  - Sends correct model in request
  - Sends prompt correctly in request
  - Handles long prompts
  - Handles empty prompts

### SupportServiceTests (18 tests)
Tests for the main support service that orchestrates FAQ matching and AI answer generation:

- **Core Functionality Tests**
  - Valid question returns answer with high confidence
  - Handles no matching FAQs gracefully
  - Works with empty FAQ list
  - Selects top 3 matching FAQs for context

- **Confidence Score Tests**
  - Short answers reduce confidence score
  - Confidence score clamped between 0 and 1
  - Long quality answers get high confidence

- **Human Review Triggers**
  - Answer containing "not sure" triggers review
  - Very short answers trigger review
  - Case-insensitive "not sure" detection

- **Keyword Extraction Tests**
  - Extracts keywords correctly from questions
  - Filters short keywords (<= 2 characters)
  - Removes punctuation from keywords
  - Handles duplicate keywords (uses distinct)
  - Handles mixed case questions

- **Error Handling Tests**
  - Handles empty questions
  - Propagates Groq service exceptions
  - Propagates FAQ service exceptions

## Technologies Used

- **xUnit** - Test framework for .NET
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - Fluent API for assertions
- **EF Core InMemory** - In-memory database for testing
- **Microsoft.NET.Test.Sdk** - Test SDK

## Running Tests

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All" to execute all tests
3. View test results and code coverage

### Command Line
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~FaqServiceTests"
dotnet test --filter "FullyQualifiedName~GroqServiceTests"
dotnet test --filter "FullyQualifiedName~SupportServiceTests"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio Code
1. Install the .NET Core Test Explorer extension
2. Tests will appear in the Test Explorer sidebar
3. Click the play button to run tests

## Test Structure

Each test follows the **Arrange-Act-Assert (AAA)** pattern:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var testData = new TestData();
    
    // Act - Execute the method being tested
    var result = await service.MethodAsync(testData);
    
    // Assert - Verify the expected outcome
    result.Should().Be(expectedValue);
}
```

## Test Database

FaqServiceTests uses an **in-memory database** that is:
- Created fresh for each test
- Automatically disposed after each test
- Isolated from other tests (no shared state)

## Mocking Strategy

### GroqServiceTests
- Mocks **HttpMessageHandler** to simulate HTTP requests/responses
- Mocks **IOptions<GroqSettings>** to inject configuration
- No actual HTTP calls to the Groq API

### SupportServiceTests
- Mocks **IFaqService** to control FAQ data
- Mocks **HttpMessageHandler** for GroqService HTTP calls
- Tests integration between FAQ matching and AI generation
- Uses real GroqService instance with mocked HTTP client

## Best Practices

1. **Test Isolation** - Each test is independent and doesn't affect others
2. **Descriptive Names** - Test names describe what is being tested and expected outcome
3. **Comprehensive Coverage** - Tests cover happy paths, edge cases, and error scenarios
4. **Fast Execution** - Tests use in-memory database and mocks for speed
5. **Maintainable** - Tests are simple and easy to understand

## Adding New Tests

When adding new tests:

1. Create test class in `Services` folder (or appropriate folder)
2. Follow the naming convention: `{ServiceName}Tests`
3. Use xUnit `[Fact]` attribute for tests
4. Use `[Theory]` and `[InlineData]` for parameterized tests
5. Use FluentAssertions for readable assertions
6. Mock dependencies using Moq
7. Follow AAA pattern

Example:
```csharp
[Fact]
public async Task MyMethod_ValidInput_ReturnsExpectedResult()
{
    // Arrange
    var service = new MyService();
    
    // Act
    var result = await service.MyMethodAsync("input");
    
    // Assert
    result.Should().NotBeNull();
    result.Should().Be("expected");
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- Fast execution (no external dependencies)
- Reliable (no flaky tests)
- Deterministic (same input = same output)
- Clear failure messages

## Test Results

? **44 tests total - All passing**
- ? FaqServiceTests: 18 tests
- ? GroqServiceTests: 12 tests  
- ? SupportServiceTests: 18 tests

## Future Improvements

- [ ] Add integration tests for controllers (AuthController, AdminController, SupportController)
- [ ] Add tests for AuthService
- [ ] Add tests for IdentitySeeder
- [ ] Implement code coverage reporting
- [ ] Add performance/load tests
- [ ] Add end-to-end API tests
- [ ] Add mutation testing
