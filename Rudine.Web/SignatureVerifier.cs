using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Rudine.Web
{
    /// <summary>
    ///     Detects if the in-lined signatures to the DocData are actually valid & have not been tempered with
    ///     At the moment, this is dead code. More research needs to be done as it does not seem to work
    ///     correctly on all windows platforms. There are 20+ different versions of the Crypt32.DLL; the
    ///     underlying engine driving this whole class.
    ///     When work properly (tested on a development windows 7 bo Microsoft Windows [Version 6.1.7601]),
    ///     it will validate the certificate against the local certificate policy server & validate
    ///     the claimed digest (kinda like a checksum) against what the document claims.
    /// </summary>
    [Obsolete("SomeDocumentName Infopath technologies are no longer supported")]
    internal static class SignatureVerifier
    {
        private const int
            X509_ASN_ENCODING = 0x00000001,
            PKCS_7_ASN_ENCODING = 0x00010000,
            PROV_RSA_FULL = 1,
            PUBLICKEYBLOB = 0x6,
            CRYPT_NEWKEYSET = 0x00000008;

        // Import statements for calls to Crypto API and Win32 API methods.
        [DllImport("Crypt32.DLL",
            EntryPoint = "CertCreateCertificateContext",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CertCreateCertificateContext(int dwCertEncodingType, byte[] pbCertEncoded, int cbCertEncoded);

        [DllImport("Crypt32.DLL",
            EntryPoint = "CertFreeCertificateContext",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        /// <summary>
        ///     This method takes an XML element which contains a base64-encoded
        ///     X509Certificate in ASN.1 format and loads it into a
        ///     System.Security.Cryptography.X509Certificates.X509Certificate
        ///     object.
        /// </summary>
        /// <param name="theCertElement">
        ///     The XML element containing
        ///     the encoded X509Certificate
        /// </param>
        /// <returns>
        ///     A new
        ///     System.Security.Cryptography.X509Certificates.X509Certificate
        ///     object
        /// </returns>
        private static X509Certificate CreateX509CertificateFromXmlElement(XmlElement theCertElement)
        {
            // Make sure the name of the element is "X509Certificate",
            // to confirm the user is making a good-faith effort to provide 
            // the proper data.
            if (theCertElement.LocalName != "X509Certificate")
                throw new Exception("Bad element name!");

            // The text of the element should be a Base64 representation 
            // of the certificate, so load it as a string.
            String base64CertificateData = theCertElement.InnerText;

            // Remove any whitespace that may be cluttering up the data.
            base64CertificateData = base64CertificateData
                .Replace("\n",
                    "")
                .Replace("\r",
                    "")
                .Replace("\f",
                    "")
                .Replace("\t",
                    "")
                .Replace(" ",
                    "");

            // Convert the data to a byte array.
            byte[] certificateData = Convert.FromBase64String(base64CertificateData);

            // Create a new X509Certificate from the data.
            return new X509Certificate(certificateData);
        }

        [DllImport("Crypt32.DLL",
            EntryPoint = "CryptAcquireContextU",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool CryptAcquireContext(ref uint phProv, string szContainer, string szProvider, int dwProvType, int dwFlags);

        [DllImport("Advapi32.DLL",
            EntryPoint = "CryptExportKey",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool CryptExportKey(uint hKey, uint hExpKey, int dwBlobType, int dwFlags, uint pbData, ref uint pdwDataLen);

        [DllImport("Crypt32.DLL",
            EntryPoint = "CryptImportPublicKeyInfoEx",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool CryptImportPublicKeyInfoEx(uint hCryptProv, uint dwCertEncodingType, IntPtr pInfo, int aiKeyAlg, int dwFlags, int pvAuxInfo, ref uint phKey);

        [DllImport("Advapi32.DLL",
            EntryPoint = "CryptReleaseContext",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool CryptReleaseContext(uint hProv, int dwFlags);

        /// This function creates and returns an RSACryptoServiceProvider 
        /// object (containing only the public key) based on the supplied 
        /// XmlDSig X509Certificate element.
        private static RSACryptoServiceProvider GetPublicKeyFromCertElement(XmlElement certElement)
        {
            // Get an X509Certificate from the XML element.
            // Return the RSA public key from the certificate.
            return GetPublicKeyFromX509Certificate(
                CreateX509CertificateFromXmlElement(certElement));
        }

        /// <summary>
        ///     This function creates and returns an RSACryptoServiceProvider
        ///     object (containing only the public key) based on the
        ///     supplied X509Certificate object.
        /// </summary>
        /// <param name="x509">
        ///     the X509Certificate object from which
        ///     to extract a public key.
        /// </param>
        /// <returns>
        ///     A System.Security.Cryptography.RSACryptoServiceProvider
        ///     object
        /// </returns>
        private static RSACryptoServiceProvider GetPublicKeyFromX509Certificate(X509Certificate x509)
        {
            // This code has been adapted from the KnowledgeBase article
            // 320602 HOW TO: Sign and Verify SignedXml Objects Using Certificates
            // http://support.microsoft.com/?id=320602

            RSACryptoServiceProvider rsacsp = null;
            uint hProv = 0;
            IntPtr pPublicKeyBlob = IntPtr.Zero;

            // Get a pointer to a CERT_CONTEXT from the raw certificate data.
            IntPtr pCertContext = IntPtr.Zero;
            pCertContext = CertCreateCertificateContext(
                X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                x509.GetRawCertData(),
                x509.GetRawCertData().Length);

            if (pCertContext == IntPtr.Zero)
            {
                Console.WriteLine("CertCreateCertificateContext failed: " + Marshal.GetLastWin32Error());
                goto Cleanup;
            }

            if (!CryptAcquireContext(ref hProv,
                    null,
                    null,
                    PROV_RSA_FULL,
                    0))
                if (!CryptAcquireContext(ref hProv,
                        null,
                        null,
                        PROV_RSA_FULL,
                        CRYPT_NEWKEYSET))
                {
                    Console.WriteLine("CryptAcquireContext failed: " + Marshal.GetLastWin32Error());
                    goto Cleanup;
                }

            // Get a pointer to the CERT_INFO structure.
            // It is the 4th DWORD of the CERT_CONTEXT structure.
            //
            //    typedef struct _CERT_CONTEXT {
            //        DWORD         dwCertEncodingType;
            //        BYTE*         pbCertEncoded;
            //        DWORD         cbCertEncoded;
            //        PCERT_INFO    pCertInfo;
            //        HCERTSTORE    hCertStore;
            //    } CERT_CONTEXT,  *PCERT_CONTEXT;
            //    typedef const CERT_CONTEXT *PCCERT_CONTEXT;
            //
            IntPtr pCertInfo = (IntPtr) Marshal.ReadInt32(pCertContext,
                12);

            // Get a pointer to the CERT_PUBLIC_KEY_INFO structure.
            // This structure is located starting at the 57th byte
            // of the CERT_INFO structure.
            // 
            //    typedef struct _CERT_INFO {
            //        DWORD                       dwVersion;
            //        CRYPT_INTEGER_BLOB          SerialNumber;
            //        CRYPT_ALGORITHM_IDENTIFIER  SignatureAlgorithm;
            //        CERT_NAME_BLOB              Issuer;
            //        FILETIME                    NotBefore;
            //        FILETIME                    NotAfter;
            //        CERT_NAME_BLOB              Subject;
            //        CERT_PUBLIC_KEY_INFO        SubjectPublicKeyInfo;
            //        CRYPT_BIT_BLOB              IssuerUniqueId;
            //        CRYPT_BIT_BLOB              SubjectUniqueId;
            //        DWORD                       cExtension;
            //        PCERT_EXTENSION             rgExtension;
            //    } CERT_INFO, *PCERT_INFO;
            // 
            IntPtr pSubjectPublicKeyInfo = (IntPtr) (pCertInfo.ToInt32() + 56);

            // Import the public key information from the certificate context
            // into a key container by passing the pointer to the 
            // SubjectPublicKeyInfo member of the CERT_INFO structure 
            // into CryptImportPublicKeyInfoEx.
            // 
            uint hKey = 0;
            if (!CryptImportPublicKeyInfoEx(hProv,
                    X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                    pSubjectPublicKeyInfo,
                    0,
                    0,
                    0,
                    ref hKey))
            {
                Console.WriteLine("CryptImportPublicKeyInfoEx failed: " + Marshal.GetLastWin32Error());
                goto Cleanup;
            }

            // Now that the key is imported into a key container use
            // CryptExportKey to export the public key to the PUBLICKEYBLOB
            // format.
            // First get the size of the buffer needed to hold the 
            // PUBLICKEYBLOB structure.
            // 
            uint dwDataLen = 0;
            if (!CryptExportKey(hKey,
                    0,
                    PUBLICKEYBLOB,
                    0,
                    0,
                    ref dwDataLen))
            {
                Console.WriteLine("CryptExportKey failed: " + Marshal.GetLastWin32Error());
                goto Cleanup;
            }

            // Then export the public key into the PUBLICKEYBLOB format.
            pPublicKeyBlob = Marshal.AllocHGlobal((int) dwDataLen);
            if (!CryptExportKey(hKey,
                    0,
                    PUBLICKEYBLOB,
                    0,
                    (uint) pPublicKeyBlob.ToInt32(),
                    ref dwDataLen))
            {
                Console.WriteLine("CryptExportKey failed: " + Marshal.GetLastWin32Error());
                goto Cleanup;
            }

            // The PUBLICKEYBLOB has the following format:
            //        BLOBHEADER blobheader;
            //        RSAPUBKEY rsapubkey;
            //        BYTE modulus[rsapubkey.bitlen/8];
            // 
            // Which can be expanded to the following:
            // 
            //        typedef struct _PUBLICKEYSTRUC {
            //            BYTE   bType;
            //            BYTE   bVersion;
            //            WORD   reserved;
            //            ALG_ID aiKeyAlg;
            //        } BLOBHEADER, PUBLICKEYSTRUC;
            //        typedef struct _RSAPUBKEY {
            //            DWORD magic;
            //            DWORD bitlen;
            //            DWORD pubexp;
            //        } RSAPUBKEY;
            //        BYTE modulus[rsapubkey.bitlen/8];

            // Get the public exponent.
            // The public exponent is located in bytes 17 through 20 of the 
            // PUBLICKEYBLOB structure.
            byte[] Exponent = new byte[4];
            Marshal.Copy((IntPtr) (pPublicKeyBlob.ToInt32() + 16),
                Exponent,
                0,
                4);
            Array.Reverse(Exponent); // Reverse the byte order.

            // Get the length of the modulus.
            // To do this extract the bit length of the modulus 
            // from the PUBLICKEYBLOB. The bit length of the modulus is at bytes 
            // 13 through 17 of the PUBLICKEYBLOB.
            int BitLength = Marshal.ReadInt32(pPublicKeyBlob,
                12);

            // Get the modulus. The modulus starts at the 21st byte of the 
            // PUBLICKEYBLOB structure and is BitLengh/8 bytes in length.
            byte[] Modulus = new byte[BitLength / 8];
            Marshal.Copy((IntPtr) (pPublicKeyBlob.ToInt32() + 20),
                Modulus,
                0,
                BitLength / 8);
            Array.Reverse(Modulus); // Reverse the byte order.

            // Put the modulus and exponent into an RSAParameters object.
            RSAParameters rsaparms = new RSAParameters
            {
                Exponent = Exponent,
                Modulus = Modulus
            };

            // Import the modulus and exponent into an RSACryptoServiceProvider
            // object via the RSAParameters object.
            rsacsp = new RSACryptoServiceProvider();
            rsacsp.ImportParameters(rsaparms);

            Cleanup:

            if (pCertContext != IntPtr.Zero)
                CertFreeCertificateContext(pCertContext);

            if (hProv != 0)
                CryptReleaseContext(hProv,
                    0);

            if (pPublicKeyBlob != IntPtr.Zero)
                Marshal.FreeHGlobal(pPublicKeyBlob);

            return rsacsp;
        }

        /// <summary>
        ///     extracts signature X509Certificate to compute document signature validities performing
        ///     http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd
        ///     against the doc's xml. If the X509 certificate policy server can't be contacted an exception will be thrown
        /// </summary>
        /// <param name="xmlDocument">white-space should have been be preserved</param>
        /// <returns>false if not signatures are in the doc or if the signatures are invalid</returns>
        public static bool Verify(string DocData) { return VerifyCount(DocData) > 0; }

        /// <summary>
        ///     extracts signature X509Certificate to compute document signature validities performing
        ///     http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd
        ///     against the doc's xml. If the X509 certificate policy server can't be contacted an exception will be thrown
        /// </summary>
        /// <param name="xmlDocument">white-space should have been be preserved</param>
        /// <returns>count of valid signatures on the document</returns>
        public static int VerifyCount(string DocData)
        {
            using (StringReader _StringReader = new StringReader(DocData))
            using (XmlTextReader _XmlTextReader = new XmlTextReader(_StringReader))
            {
                // Doc can't be submitted without signatures if they are they are present
                XmlDocument _XmlDocument = new XmlDocument { PreserveWhitespace = true };
                _XmlDocument.Load(_XmlTextReader);
                // Get an XmlNodeList of all the signatures in the document.
                XmlNodeList _XmlNodeList = _XmlDocument.SelectNodes("//*[local-name()='Signature']");

                //if (_XmlNodeList != null)
                //    foreach (XmlNode theSignature in _XmlNodeList)
                //    {
                //        // Create a new SignedXML object.
                //        SignedXml _SignedXml = new SignedXml(_XmlDocument);

                //        //Load the next signature.
                //        _SignedXml.LoadXml((XmlElement)theSignature);

                //        XmlElement _CertXmlElement = (XmlElement)theSignature
                //            .ChildNodes.Cast<XmlNode>().First(node => node.LocalName == "KeyInfo")
                //            .ChildNodes.Cast<XmlNode>().First(node => node.LocalName == "X509Data")
                //            .ChildNodes.Cast<XmlNode>().First(node => node.LocalName == "X509Certificate");

                //        // Get an RSA Public Key from the certificate.
                //        // See the KeyFromCert.cs file below for details
                //        // Verify the signature.
                //        if (!_SignedXml.CheckSignature(
                //            GetPublicKeyFromCertElement(_CertXmlElement)))
                //            return false;
                //    }

                return
                    _XmlNodeList == null
                        ? 0
                        : _XmlNodeList.Count;
            }
        }
    }
}