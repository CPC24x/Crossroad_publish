using System.Threading.Tasks;
using LinqToDB;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Seo;

namespace Nop.Plugin.Crossroad.Integration.Services.Picture;

public class PictureExtendedService : PictureService, IPictureExtendedService
{
    private readonly IRepository<Core.Domain.Media.Picture> _pictureRepository;
    private readonly IRepository<ProductPicture> _productPictureRepository;

    public PictureExtendedService(IDownloadService downloadService, 
        IHttpContextAccessor httpContextAccessor, 
        ILogger logger, 
        INopFileProvider fileProvider, 
        IProductAttributeParser productAttributeParser, 
        IRepository<Core.Domain.Media.Picture> pictureRepository, 
        IRepository<PictureBinary> pictureBinaryRepository, 
        IRepository<ProductPicture> productPictureRepository, 
        ISettingService settingService, IUrlRecordService urlRecordService, IWebHelper webHelper, MediaSettings mediaSettings) : base(downloadService, httpContextAccessor, logger, fileProvider, productAttributeParser, pictureRepository, pictureBinaryRepository, productPictureRepository, settingService, urlRecordService, webHelper, mediaSettings)
    {
        _pictureRepository = pictureRepository;
        _productPictureRepository = productPictureRepository;
    }

    public async Task<Core.Domain.Media.Picture> GetPictureBySeoName(string seoName) => await _pictureRepository.Table.FirstOrDefaultAsync(p => p.SeoFilename == seoName);

    public async Task<bool> IsProductPictureExists(int pictureId, int productId) => await _productPictureRepository.Table.AnyAsync(p => p.PictureId == pictureId && p.ProductId == productId);
}