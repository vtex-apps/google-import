using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ListFilesResponse
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("incompleteSearch")]
        public bool IncompleteSearch { get; set; }

        [JsonProperty("files")]
        public List<GoogleFile> Files { get; set; }
    }

    public class GoogleFile
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("starred")]
        public bool Starred { get; set; }

        [JsonProperty("trashed")]
        public bool Trashed { get; set; }

        [JsonProperty("explicitlyTrashed")]
        public bool ExplicitlyTrashed { get; set; }

        [JsonProperty("parents")]
        public List<string> Parents { get; set; }

        [JsonProperty("spaces")]
        public List<string> Spaces { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("webViewLink")]
        public Uri WebViewLink { get; set; }

        [JsonProperty("iconLink")]
        public Uri IconLink { get; set; }

        [JsonProperty("hasThumbnail")]
        public bool HasThumbnail { get; set; }

        [JsonProperty("thumbnailVersion")]
        public string ThumbnailVersion { get; set; }

        [JsonProperty("viewedByMe")]
        public bool ViewedByMe { get; set; }

        [JsonProperty("viewedByMeTime")]
        public DateTimeOffset? ViewedByMeTime { get; set; }

        [JsonProperty("createdTime")]
        public DateTimeOffset CreatedTime { get; set; }

        [JsonProperty("modifiedTime")]
        public DateTimeOffset ModifiedTime { get; set; }

        [JsonProperty("modifiedByMeTime")]
        public DateTimeOffset? ModifiedByMeTime { get; set; }

        [JsonProperty("modifiedByMe")]
        public bool ModifiedByMe { get; set; }

        [JsonProperty("owners")]
        public List<LastModifyingUser> Owners { get; set; }

        [JsonProperty("lastModifyingUser")]
        public LastModifyingUser LastModifyingUser { get; set; }

        [JsonProperty("shared")]
        public bool Shared { get; set; }

        [JsonProperty("ownedByMe")]
        public bool OwnedByMe { get; set; }

        [JsonProperty("capabilities")]
        public Dictionary<string, bool> Capabilities { get; set; }

        [JsonProperty("viewersCanCopyContent")]
        public bool ViewersCanCopyContent { get; set; }

        [JsonProperty("copyRequiresWriterPermission")]
        public bool CopyRequiresWriterPermission { get; set; }

        [JsonProperty("writersCanShare")]
        public bool WritersCanShare { get; set; }

        [JsonProperty("permissions")]
        public List<Permission> Permissions { get; set; }

        [JsonProperty("permissionIds")]
        public List<string> PermissionIds { get; set; }

        [JsonProperty("folderColorRgb")]
        public string FolderColorRgb { get; set; }

        [JsonProperty("quotaBytesUsed")]
        public long QuotaBytesUsed { get; set; }

        [JsonProperty("isAppAuthorized")]
        public bool IsAppAuthorized { get; set; }

        [JsonProperty("webContentLink")]
        public Uri WebContentLink { get; set; }

        [JsonProperty("thumbnailLink")]
        public Uri ThumbnailLink { get; set; }

        [JsonProperty("originalFilename")]
        public string OriginalFilename { get; set; }

        [JsonProperty("fullFileExtension")]
        public string FullFileExtension { get; set; }

        [JsonProperty("fileExtension")]
        public string FileExtension { get; set; }

        [JsonProperty("md5Checksum")]
        public string Md5Checksum { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }

        [JsonProperty("headRevisionId")]
        public string HeadRevisionId { get; set; }

        [JsonProperty("imageMediaMetadata")]
        public ImageMediaMetadata ImageMediaMetadata { get; set; }
    }

    public class ImageMediaMetadata
    {
        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("rotation")]
        public long Rotation { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("colorSpace")]
        public string ColorSpace { get; set; }
    }

    public class LastModifyingUser
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("photoLink")]
        public Uri PhotoLink { get; set; }

        [JsonProperty("me")]
        public bool Me { get; set; }

        [JsonProperty("permissionId")]
        public string PermissionId { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }
    }

    public class Permission
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("photoLink")]
        public Uri PhotoLink { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
    }
}
