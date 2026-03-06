using FluentAssertions;
using TechChallenge.Web.Domain;

namespace TechChallenge.Tests.Domain;

/// <summary>Tests for PagedResult pagination metadata calculations.</summary>
public class PagedResultTests
{
    [Fact]
    public void TotalPages_ShouldRoundUp()
    {
        var result = new PagedResult<string>(new(), TotalCount: 11, Page: 1, PageSize: 5);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void HasPrevious_OnFirstPage_ShouldBeFalse()
    {
        var result = new PagedResult<string>(new(), TotalCount: 100, Page: 1, PageSize: 10);

        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasNext_OnLastPage_ShouldBeFalse()
    {
        var result = new PagedResult<string>(new(), TotalCount: 10, Page: 1, PageSize: 10);

        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_WhenMorePagesExist_ShouldBeTrue()
    {
        var result = new PagedResult<string>(new(), TotalCount: 100, Page: 1, PageSize: 10);

        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void EmptyResult_ShouldHaveZeroPages()
    {
        var result = new PagedResult<string>(new(), TotalCount: 0, Page: 1, PageSize: 10);

        result.TotalPages.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
    }
}