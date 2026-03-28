using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Portal.Security;

internal static class DeploymentJobSecretMapper
{
    public static DeploymentJob ForStorage(DeploymentJob source, IDataProtectorFacade protector)
    {
        var clone = Clone(source);

        if (!string.IsNullOrWhiteSpace(clone.PfxData))
        {
            clone.PfxData = protector.Protect(clone.PfxData);
        }

        if (!string.IsNullOrWhiteSpace(clone.PfxPassword))
        {
            clone.PfxPassword = protector.Protect(clone.PfxPassword);
        }

        return clone;
    }

    public static DeploymentJob ForCollector(DeploymentJob source, IDataProtectorFacade protector)
    {
        var clone = Clone(source);

        if (!string.IsNullOrWhiteSpace(clone.PfxData))
        {
            clone.PfxData = protector.Unprotect(clone.PfxData);
        }

        if (!string.IsNullOrWhiteSpace(clone.PfxPassword))
        {
            clone.PfxPassword = protector.Unprotect(clone.PfxPassword);
        }

        return clone;
    }

    public static DeploymentJob ForUiList(DeploymentJob source)
    {
        var clone = Clone(source);
        clone.PfxData = null;
        clone.PfxPassword = null;
        return clone;
    }

    private static DeploymentJob Clone(DeploymentJob source)
    {
        return new DeploymentJob
        {
            Id = source.Id,
            CertificateId = source.CertificateId,
            TargetHostname = source.TargetHostname,
            StoreLocation = source.StoreLocation,
            Status = source.Status,
            ErrorMessage = source.ErrorMessage,
            CreatedAt = source.CreatedAt,
            CompletedAt = source.CompletedAt,
            PfxData = source.PfxData,
            PfxPassword = source.PfxPassword
        };
    }
}