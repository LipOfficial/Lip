﻿using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using Lip.Context;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lip.Tests;

public class LipListTests
{
    [Theory]
    [InlineData("win-x64")]
    [InlineData("linux-x64")]
    [InlineData("osx-x64")]
    public async Task List_ReturnsListItems(string runtimeIdentifier)
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "tooth_lock.json", new MockFileData($$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": [
                    {
                        "format_version": 3,
                        "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                        "tooth": "example.com/pkg1",
                        "version": "1.0.0",
                        "variants": [
                            {
                                "label": "variant1",
                                "platform": "{{runtimeIdentifier}}"
                            }
                        ]
                    },
                    {
                        "format_version": 3,
                        "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                        "tooth": "example.com/pkg2",
                        "version": "1.0.1",
                        "variants": [
                            {
                                "label": "variant2",
                                "platform": "{{runtimeIdentifier}}"
                            }
                        ]
                    }
                ],
                "locks": [
                    {
                        "tooth": "example.com/pkg1",
                        "variant": "variant1",
                        "version": "1.0.0",
                    }
                ]
            }
            """) }
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.RuntimeIdentifier).Returns(runtimeIdentifier);

        Lip lip = new(new(), context.Object);

        // Act.
        List<Lip.ListItem> listItems = await lip.List(new());

        // Assert.
        Assert.Equal(2, listItems.Count);
        Assert.Equal("example.com/pkg1", listItems[0].Manifest.ToothPath);
        Assert.Equal("1.0.0", listItems[0].Manifest.VersionText.ToString());
        Assert.Equal("variant1", listItems[0].Manifest.Variants![0].VariantLabel);
        Assert.True(listItems[0].Locked);
        Assert.Equal("example.com/pkg2", listItems[1].Manifest.ToothPath);
        Assert.Equal("1.0.1", listItems[1].Manifest.VersionText.ToString());
        Assert.Equal("variant2", listItems[1].Manifest.Variants![0].VariantLabel);
        Assert.False(listItems[1].Locked);
    }

    [Fact]
    public async Task List_LockFileNotExists_ReturnsEmptyList()
    {
        // Arrange.
        var fileSystem = new MockFileSystem();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        Lip lip = new(new(), context.Object);

        // Act.
        List<Lip.ListItem> listItems = await lip.List(new());

        // Assert.
        Assert.Empty(listItems);
    }

    [Fact]
    public async Task List_MismatchedToothPath_ReturnsListItems()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "tooth_lock.json", new MockFileData("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": [
                    {
                        "format_version": 3,
                        "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                        "tooth": "example.com/pkg1",
                        "version": "1.0.0",
                        "variants": [
                            {
                                "label": "variant1",
                                "platform": "win-x64"
                            }
                        ]
                    }
                ],
                "locks": [
                    {
                        "tooth": "example.com/pkg2",
                        "variant": "variant1",
                        "version": "1.0.0",
                    }
                ]
            }
            """) }
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.RuntimeIdentifier).Returns("win-x64");

        Lip lip = new(new(), context.Object);

        // Act.
        List<Lip.ListItem> listItems = await lip.List(new());

        // Assert.
        Assert.Single(listItems);
        Assert.Equal("example.com/pkg1", listItems[0].Manifest.ToothPath);
        Assert.Equal("1.0.0", listItems[0].Manifest.Version.ToString());
        Assert.Equal("variant1", listItems[0].Manifest.Variants![0].VariantLabel);
        Assert.False(listItems[0].Locked);
    }

    [Fact]
    public async Task List_MismatchedVersion_ReturnsListItems()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "tooth_lock.json", new MockFileData("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": [
                    {
                        "format_version": 3,
                        "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                        "tooth": "example.com/pkg1",
                        "version": "1.0.0",
                        "variants": [
                            {
                                "label": "variant1",
                                "platform": "win-x64"
                            }
                        ]
                    }
                ],
                "locks": [
                    {
                        "tooth": "example.com/pkg1",
                        "variant": "variant1",
                        "version": "1.0.1",
                    }
                ]
            }
            """) }
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.RuntimeIdentifier).Returns("win-x64");

        Lip lip = new(new(), context.Object);

        // Act.
        List<Lip.ListItem> listItems = await lip.List(new());

        // Assert.
        Assert.Single(listItems);
        Assert.Equal("example.com/pkg1", listItems[0].Manifest.ToothPath);
        Assert.Equal("1.0.0", listItems[0].Manifest.Version.ToString());
        Assert.Equal("variant1", listItems[0].Manifest.Variants![0].VariantLabel);
        Assert.False(listItems[0].Locked);
    }

    [Fact]
    public async Task List_MismatchedVariantLabel_ReturnsListItems()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "tooth_lock.json", new MockFileData("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": [
                    {
                        "format_version": 3,
                        "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                        "tooth": "example.com/pkg1",
                        "version": "1.0.0",
                        "variants": [
                            {
                                "label": "variant1",
                                "platform": "win-x64"
                            }
                        ]
                    }
                ],
                "locks": [
                    {
                        "tooth": "example.com/pkg1",
                        "variant": "variant2",
                        "version": "1.0.0",
                    }
                ]
            }
            """) }
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.RuntimeIdentifier).Returns("win-x64");

        Lip lip = new(new(), context.Object);

        // Act.
        List<Lip.ListItem> listItems = await lip.List(new());

        // Assert.
        Assert.Single(listItems);
        Assert.Equal("example.com/pkg1", listItems[0].Manifest.ToothPath);
        Assert.Equal("1.0.0", listItems[0].Manifest.Version.ToString());
        Assert.Equal("variant1", listItems[0].Manifest.Variants![0].VariantLabel);
        Assert.False(listItems[0].Locked);
    }
}
