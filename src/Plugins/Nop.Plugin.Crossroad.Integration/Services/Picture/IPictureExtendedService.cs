using System.Threading.Tasks;
using Nop.Services.Media;

namespace Nop.Plugin.Crossroad.Integration.Services.Picture;

public interface IPictureExtendedService : IPictureService
{
    Task<Core.Domain.Media.Picture> GetPictureBySeoName(string seoName);

    Task<bool> IsProductPictureExists(int pictureId, int productId);
}