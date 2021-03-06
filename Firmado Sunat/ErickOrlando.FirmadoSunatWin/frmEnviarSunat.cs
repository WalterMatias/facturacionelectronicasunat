﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ErickOrlando.FirmadoSunat;

namespace ErickOrlando.FirmadoSunatWin
{
    public partial class FrmEnviarSunat : Form
    {
        public FrmEnviarSunat()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Seleccione un Documento XML SUNAT";
                    ofd.Filter = "Documentos XML sin firma (*.xml)|*.xml";
                    ofd.FilterIndex = 1;
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtSource.Text = ofd.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGen_Click(object sender, EventArgs e)
        {

            try
            {
                string codigoTipoDoc;
                string letraTipoDoc;
                switch (cboTipoDoc.SelectedIndex)
                {
                    case 0:
                        letraTipoDoc = "F";
                        codigoTipoDoc = "01";
                        break;
                    case 1:
                        letraTipoDoc = "B";
                        codigoTipoDoc = "03";
                        break;
                    case 2:
                        letraTipoDoc = "F";
                        codigoTipoDoc = "07";
                        break;
                    case 3:
                        letraTipoDoc = "F";
                        codigoTipoDoc = "08";
                        break;
                    case 4:
                        letraTipoDoc = "R";
                        codigoTipoDoc = "20";
                        break;
                    case 5:
                        letraTipoDoc = "P";
                        codigoTipoDoc = "40";
                        break;
                    default:
                        letraTipoDoc = "F";
                        codigoTipoDoc = "01";
                        break;
                }
                // Una vez validado el XML seleccionado procedemos con obtener el Certificado.
                var serializar = new Serializador
                {
                    RutaCertificadoDigital = Convert.ToBase64String(File.ReadAllBytes(txtRutaCertificado.Text)),
                    PasswordCertificado = txtPassCertificado.Text,
                    TipoDocumento = rbRetenciones.Checked ? 0 : 1
                };

                using (var conexion = new ConexionSunat(txtNroRuc.Text, txtUsuarioSol.Text,
                    txtClaveSol.Text, rbRetenciones.Checked ? "ServicioSunatRetenciones" : string.Empty))
                {

                    //var archivoOriginal = Path.GetFileName(txtSource.Text);

                    var byteArray = File.ReadAllBytes(txtSource.Text);

                    Cursor = Cursors.WaitCursor;

                    // Firmamos el XML.
                    var tramaFirmado = serializar.FirmarXml(Convert.ToBase64String(byteArray));
                    // Le damos un nuevo nombre al archivo
                    var nombreArchivo = $"{txtNroRuc.Text}-{codigoTipoDoc}-{letraTipoDoc}001-001";
                    // Ahora lo empaquetamos en un ZIP.
                    var tramaZip = serializar.GenerarZip(tramaFirmado, nombreArchivo);

                    var resultado = conexion.EnviarDocumento(tramaZip, $"{nombreArchivo}.zip");

                    if (resultado.Item2)
                    {
                        var returnByte = Convert.FromBase64String(resultado.Item1);

                        var rutaArchivo = $"{Directory.GetCurrentDirectory()}R.{nombreArchivo}.zip";
                        var fs = new FileStream(rutaArchivo, FileMode.Create);
                        fs.Write(returnByte, 0, returnByte.Length);
                        fs.Close();

                        txtResult.Text = "Proceso enviado a SUNAT correctamente!";

                        Process.Start(rutaArchivo);
                    }
                    else
                        txtResult.Text = resultado.Item1;

                }
            }
            catch (System.ServiceModel.FaultException exSer)
            {
                txtResult.Text = exSer.ToString();
            }
            catch (Exception ex)
            {
                txtResult.Text = ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnBrowseCert_Click(object sender, EventArgs e)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Seleccione un Certificado";
                    ofd.Filter = "Certificados Digitales (*.cer;*.pfx;*.p7b)|*.cer;*.pfx;*.p7b";
                    ofd.FilterIndex = 1;
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtRutaCertificado.Text = ofd.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
