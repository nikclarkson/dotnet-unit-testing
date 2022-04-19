# DotNet Unit Testing 

## What is Unit Testing? 

**Unit Testing** in this context refers to writing automated functional tests in code that involve no external dependencies.  If these tests were to include things like accessing a database, calling another service, etc then it would be more along the lines of an **Integration Testing**. 
> The terms **Unit Test** & **Integration Test** are sometimes defined differently, but for the purposes of this exploration we will be adopting the above definitions. 

## What are the motivations behind Unit Testing? 

In the simplest terms - *to help the humans*

* Confidence in knowing that what has been developed meets the acceptance criteria. 

* Confidence that bugs won't regress and be found again

* Readability of the code - documents the code base allows new delovopers and your future self a alike to understand the code

* Maintainability of the code - reduces the fear of change. expanded requirements, enhancements, refactoriings are all things that happen in the course of a piece of software's lifetime. We should have to live in fear of making changes. 

* Reasonability - we can determine what is reasonable and feasible when looking for a bug and collaborating on it; correcting incorrect assumptions in previous tests

* Communication - We can name tests and assertion in plain language that can match the requirements and allow us to have better collaboration with less technical audiences. 

* Not all but a lot of poorly structured code is difficult to unit test properly. 

## How do we write Unit Tests in .Net? 


### xUnit 

We will use **xUnit** as our unit testing framework of choice. It has a long, well maintained history and integrates nicely in the dotnet ecosystem. 

The major feature of **xUnit** that we're intrested is leveraging its *[attributes]* that allows our build tooling to recognize and execute code as unit tests.  

**xUnit** provides us with a `[Fact]` attribute that is used to indicate test that is always true. It also provides `[Theory]` which indicates that a test is only true for a particular set of data. 

The following is an example of a `[Fact]`

``` csharp 
    [Fact]
    public void A_Test()
    {
        var calculator = new Calculator("la calculadora"); 

        var result = calculator.Sum(1, 1);

        Assert.Equal(2, result);
    }
``` 

In the above test we've created an instance of the `Calculator` class. It is our SUT (Subject Under Test).  This test expects that the assertion will always hold true for the give call to the calulator's `Sum` method. 

If we wanted to test the `Sum` method with different input parameters we could do so, but we would encounter some values that wouldn't pass if you simply added the numbers yourself and used that answer as the expected value. Since our calculator uses Int32 types for parameters and return type we can exceed the int.MaxValue to contrive an example of a situation that might not hold true for all values. We could avoid this by using a `[Theory]` and only testing only the values we expect to hold true or even possibly test all values but adjust the expect values accordingly.

``` csharp
    [Theory]
    [InlineData(2147483646, 1, 2147483647)] // This test will pass
    [InlineData(2147483647, 1, 2147483648)] // This test will fail since 2147483647 is int.MaxValue
    public void Theory_Test(int a, int b, int answer)
    {
        var calculator = new Calculator("la calculadora"); 

        var result = calculator.Sum(a, b);

        Assert.Equal(answer, result);
    }
```

There are additional ways to provide test data to theories besides the `[InlineData()]` attribute. `[MemberData]` and `[ClassData]` are also provided by **xUnit**. The usage details for these are left to your exploration should the need arise. 


A common pattern emerges
``` csharp
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
```

We *Arrange* the test by creating an instance of our SUT(Subject Under Test) the `Calculator`.  If we had any other depedencies to satisfy this is where we would handle that. 

Now we can *Act* on the **SUT** by calling its `Sum()` method.  

We gain value from our tests by *Assert*ing that something in partular has happened and we have received the expected result.  Being specific and intentional with our Asserts is important.  Code coverage numbers can be achived through *Arrange* & *Act* but the value of a test is realized in the quality of its *Assert*(ions). 

