namespace Domain.Enums;

public enum JobCardStatus : short
{
    NuevaSolicitud = 0,
    PedidoRealizado = 1,
    PedidoRecibido = 2,
    EsperandoAprobacion = 3,
    EnProceso = 4,
    ClienteInformado = 5,
    ListoParaRecoger = 6,
    Pagado = 7
}
