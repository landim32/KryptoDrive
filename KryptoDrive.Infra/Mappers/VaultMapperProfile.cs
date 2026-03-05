using AutoMapper;
using KryptoDrive.Domain.Models;
using KryptoDrive.DTO.DTOs;

namespace KryptoDrive.Infra.Mappers
{
    public class VaultMapperProfile : Profile
    {
        public VaultMapperProfile()
        {
            CreateMap<MediaFile, MediaFileInfo>();
            CreateMap<SecureFolder, SecureFolderInfo>();
            CreateMap<MediaCatalog, MediaCatalogInfo>();
        }
    }
}
