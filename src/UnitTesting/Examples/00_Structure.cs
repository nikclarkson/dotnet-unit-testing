using Xunit;

namespace Examples;

public class Structure
{
    [Fact]
    public void Fact_Test()
    {
        // Arrange
        var calculator = new Calculator("la calculadora"); 

        // Act
        var result = calculator.Sum(1, 1);

        // Assert
        Assert.Equal(2, result);

    }

    [Theory]
    [InlineData(2147483646, 1, 2147483647)]
    [InlineData(2147483647, 1, 2147483648)]
    public void Theory_Test(int a, int b, int answer)
    {
        // Arrange
        var calculator = new Calculator("la calculadora"); 

        // Act
        var result = calculator.Sum(a, b);

        // Assert
        Assert.Equal(answer, result);
    }

}

public class Calculator
{
    public string Name { get; init; }

    public Calculator(string name)
    {
        Name = name;
    }

    public int Sum(int a, int b)
    {
        return a + b;
    }
}