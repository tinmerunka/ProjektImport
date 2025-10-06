using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace InventoryManagementAPI.DTOs
{
    public class FiscalizationTestRequest
    {
        [Required]
        public int InvoiceId { get; set; }
    }

    public class FiscalizationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Jir { get; set; }
        public string? Zki { get; set; }
        public string? RawResponse { get; set; }
        public DateTime? FiscalizedAt { get; set; }
    }

    // XML DTOs for FINA communication
    [XmlRoot("tns:RacunZahtjev", Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public class InvoiceFiscalRequest
    {
        [XmlElement("tns:Zaglavlje")]
        public RequestHeader Header { get; set; } = new RequestHeader();

        [XmlElement("tns:Racun")]
        public FiscalInvoice Invoice { get; set; } = new FiscalInvoice();
    }

    public class RequestHeader
    {
        [XmlElement("tns:IdPoruke")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        [XmlElement("tns:DatumVrijeme")]
        public string DateTime { get; set; } = System.DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss");
    }

    public class FiscalInvoice
    {
        [XmlElement("tns:Oib")]
        public string CompanyOib { get; set; } = string.Empty;

        [XmlElement("tns:USustPdv")]
        public bool InVatSystem { get; set; } = true;

        [XmlElement("tns:DatVrijeme")]
        public string IssueDateTime { get; set; } = string.Empty;

        [XmlElement("tns:OznSlijBr")]
        public string SequentialMark { get; set; } = string.Empty; // Invoice number

        [XmlElement("tns:BrRac")]
        public BusinessLocation BusinessLocation { get; set; } = new BusinessLocation();

        [XmlElement("tns:Pdv")]
        public List<TaxItem> TaxItems { get; set; } = new List<TaxItem>();

        [XmlElement("tns:Pnp")]
        public List<NonTaxableItem> NonTaxableItems { get; set; } = new List<NonTaxableItem>();

        [XmlElement("tns:IznosUkupno")]
        public string TotalAmount { get; set; } = string.Empty;

        [XmlElement("tns:NacinPlac")]
        public string PaymentMethod { get; set; } = string.Empty;

        [XmlElement("tns:OibKupca")]
        public string CustomerOib { get; set; } = string.Empty;

        [XmlElement("tns:NakDost")]
        public bool SubsequentDelivery { get; set; } = false;

        [XmlElement("tns:ParagonBrRac")]
        public string ParagonInvoiceNumber { get; set; } = string.Empty;

        [XmlElement("tns:SpecNamj")]
        public string SpecialPurpose { get; set; } = string.Empty;

        [XmlElement("tns:ZastKod")]
        public string SecurityCode { get; set; } = string.Empty; // ZKI
    }

    public class BusinessLocation
    {
        [XmlElement("tns:BrPoslovne")]
        public string BusinessUnitNumber { get; set; } = string.Empty; // InvoiceParam1

        [XmlElement("tns:BrNapl")]
        public string CashRegisterNumber { get; set; } = string.Empty; // InvoiceParam2
    }

    public class TaxItem
    {
        [XmlElement("tns:Stopa")]
        public string Rate { get; set; } = string.Empty;

        [XmlElement("tns:Osnovica")]
        public string Base { get; set; } = string.Empty;

        [XmlElement("tns:Iznos")]
        public string Amount { get; set; } = string.Empty;
    }

    public class NonTaxableItem
    {
        [XmlElement("tns:Naziv")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("tns:Iznos")]
        public string Amount { get; set; } = string.Empty;
    }

    // Response DTOs
    [XmlRoot("tns:RacunOdgovor", Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public class InvoiceFiscalResponse
    {
        [XmlElement("tns:Zaglavlje")]
        public ResponseHeader Header { get; set; } = new ResponseHeader();

        [XmlElement("tns:Jir")]
        public string Jir { get; set; } = string.Empty;

        [XmlElement("tns:Greske")]
        public ResponseErrors Errors { get; set; } = new ResponseErrors();
    }

    public class ResponseHeader
    {
        [XmlElement("tns:IdPoruke")]
        public string MessageId { get; set; } = string.Empty;

        [XmlElement("tns:DatumVrijeme")]
        public string DateTime { get; set; } = string.Empty;
    }

    public class ResponseErrors
    {
        [XmlElement("tns:Greska")]
        public List<ResponseError> ErrorList { get; set; } = new List<ResponseError>();
    }

    public class ResponseError
    {
        [XmlElement("tns:SifGreske")]
        public string ErrorCode { get; set; } = string.Empty;

        [XmlElement("tns:PorukaGreske")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}