using System.ComponentModel.DataAnnotations;
using LTLaserBlogMvc.Models;
namespace LTLaserBlogMvc.ViewModels;
public sealed class LoginVm { 
    [Required,EmailAddress] 
    public string Email {get; set;} = ""; 

    [Required,DataType(DataType.Password)] 
    public string Password {get; set;} = ""; 
}
public sealed class CreateAdminVm { 
    [Required,EmailAddress] 
    public string Email {get;set;}=""; 
    
    [Required,MinLength(12),DataType(DataType.Password)] 
    public string Password {get;set;}=""; 
    
    [Required,Compare(nameof(Password)),DataType(DataType.Password)] 
    public string ConfirmPassword {get;set;}=""; 
}
public sealed class PostBlockVm {
    public int? Id { get; set; }
    public PostBlockType BlockType {get;set;} = PostBlockType.Paragraph;
    public string? Content {get;set;}="";
    [MaxLength(300)] public string? Caption {get;set;}
    public IFormFile? Image {get;set;}
    public bool KeepExistingImage {get;set;} = true;
    public bool HasExistingImage {get;set;}
}
public sealed class PostVm {
    public int? Id {get;set;}
    [Required,MaxLength(180)] public string Title {get;set;}="";
    [Required,MaxLength(500)] public string Summary {get;set;}="";
    public IFormFile? CoverImage {get;set;}
    public bool KeepCoverImage {get;set;}=true;
    public bool HasCoverImage {get;set;}
    public List<PostBlockVm> Blocks {get;set;}=[];
}

public sealed class PostListVm {
    public List<Post> Posts { get; set; } = [];
    public string? SearchQuery { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public int Skip { get; set; }
}

