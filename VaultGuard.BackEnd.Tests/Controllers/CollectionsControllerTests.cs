using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PasswordManager.API.Controllers;
using PasswordManager.Models.DTOs;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.BackEnd.Tests.Controllers;

[TestFixture]
public class CollectionsControllerTests
{
    private Mock<ICollectionApiService> _mockCollectionService = null!;
    private Mock<ILogger<CollectionsController>> _mockLogger = null!;
    private CollectionsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockCollectionService = new Mock<ICollectionApiService>();
        _mockLogger = new Mock<ILogger<CollectionsController>>();
        _controller = new CollectionsController(_mockCollectionService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsCollections_ShouldReturnOkWithCollections()
    {
        // Arrange
        var collections = new List<CollectionDto>
        {
            new CollectionDto { Id = 1, Name = "Collection 1", Description = "Description 1" },
            new CollectionDto { Id = 2, Name = "Collection 2", Description = "Description 2" }
        };

        _mockCollectionService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult?.Value, Is.EqualTo(collections));

        _mockCollectionService.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAll_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockCollectionService.Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var errorResult = result.Result as ObjectResult;
        Assert.That(errorResult?.StatusCode, Is.EqualTo(500));
        Assert.That(errorResult?.Value, Is.EqualTo("An error occurred while retrieving collections"));
    }

    [Test]
    public async Task GetById_WithValidId_ShouldReturnOkWithCollection()
    {
        // Arrange
        var collectionId = 1;
        var collection = new CollectionDto 
        { 
            Id = collectionId, 
            Name = "Test Collection", 
            Description = "Test Description" 
        };

        _mockCollectionService.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.GetById(collectionId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult?.Value, Is.EqualTo(collection));

        _mockCollectionService.Verify(x => x.GetByIdAsync(collectionId), Times.Once);
    }

    [Test]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = 999;

        _mockCollectionService.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((CollectionDto)null);

        // Act
        var result = await _controller.GetById(collectionId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo($"Collection with ID {collectionId} not found"));
    }

    [Test]
    public async Task GetById_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = 1;
        _mockCollectionService.Setup(x => x.GetByIdAsync(collectionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetById(collectionId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var errorResult = result.Result as ObjectResult;
        Assert.That(errorResult?.StatusCode, Is.EqualTo(500));
        Assert.That(errorResult?.Value, Does.Contain("An error occurred"));
    }

    [Test]
    public async Task Create_WithValidCollection_ShouldReturnCreatedResult()
    {
        // Arrange
        var createDto = new CreateCollectionDto 
        { 
            Name = "New Collection", 
            Description = "New Description" 
        };

        var createdCollection = new CollectionDto 
        { 
            Id = 1, 
            Name = createDto.Name, 
            Description = createDto.Description 
        };

        _mockCollectionService.Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdCollection);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult?.Value, Is.EqualTo(createdCollection));
        Assert.That(createdResult?.ActionName, Is.EqualTo(nameof(CollectionsController.GetById)));
        Assert.That(createdResult?.RouteValues?["id"], Is.EqualTo(createdCollection.Id));

        _mockCollectionService.Verify(x => x.CreateAsync(createDto), Times.Once);
    }

    [Test]
    public async Task Create_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateCollectionDto { Name = "" }; // Invalid - empty name
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Create_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var createDto = new CreateCollectionDto 
        { 
            Name = "New Collection", 
            Description = "New Description" 
        };

        _mockCollectionService.Setup(x => x.CreateAsync(createDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var errorResult = result.Result as ObjectResult;
        Assert.That(errorResult?.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task Update_WithValidIdAndCollection_ShouldReturnOkResult()
    {
        // Arrange
        var collectionId = 1;
        var updateDto = new UpdateCollectionDto 
        { 
            Name = "Updated Collection", 
            Description = "Updated Description" 
        };

        var updatedCollection = new CollectionDto 
        { 
            Id = collectionId, 
            Name = updateDto.Name, 
            Description = updateDto.Description 
        };

        _mockCollectionService.Setup(x => x.UpdateAsync(collectionId, updateDto))
            .ReturnsAsync(updatedCollection);

        // Act
        var result = await _controller.Update(collectionId, updateDto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult?.Value, Is.EqualTo(updatedCollection));

        _mockCollectionService.Verify(x => x.UpdateAsync(collectionId, updateDto), Times.Once);
    }

    [Test]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = 999;
        var updateDto = new UpdateCollectionDto 
        { 
            Name = "Updated Collection", 
            Description = "Updated Description" 
        };

        _mockCollectionService.Setup(x => x.UpdateAsync(collectionId, updateDto))
            .ReturnsAsync((CollectionDto)null);

        // Act
        var result = await _controller.Update(collectionId, updateDto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo($"Collection with ID {collectionId} not found"));
    }

    [Test]
    public async Task Delete_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var collectionId = 1;

        _mockCollectionService.Setup(x => x.DeleteAsync(collectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(collectionId);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        _mockCollectionService.Verify(x => x.DeleteAsync(collectionId), Times.Once);
    }

    [Test]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = 999;

        _mockCollectionService.Setup(x => x.DeleteAsync(collectionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(collectionId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo($"Collection with ID {collectionId} not found"));
    }

    [Test]
    public async Task Delete_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = 1;
        _mockCollectionService.Setup(x => x.DeleteAsync(collectionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Delete(collectionId);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var errorResult = result as ObjectResult;
        Assert.That(errorResult?.StatusCode, Is.EqualTo(500));
    }

    // Note: SetAsDefault functionality has been moved to collections service tests
    // as the controller method may not exist in the current API implementation
}