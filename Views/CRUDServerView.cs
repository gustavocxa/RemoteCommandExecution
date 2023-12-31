﻿using RemoteCommandExecution.Models;
using Renci.SshNet;
using System.Diagnostics;
using System.Xml.Serialization;

namespace RemoteCommandExecution.Views
{
    public partial class CRUDServerView : Form
    {
        public int _serverId { get; set; } = 0;
        public string _serverName { get; set; }
        public event EventHandler ServerAdded;

        public CRUDServerView(string serverName = "")
        {
            InitializeComponent();
            _serverName = serverName;

            if (!String.IsNullOrEmpty(_serverName))
            {
                textBoxName.Enabled = false;
                textBoxName.Text = "Cargando...";
                textBoxIp.Enabled = false;
                textBoxIp.Text = "Cargando...";
                textBoxUser.Enabled = false;
                textBoxUser.Text = "Cargando...";
                textBoxPassword.Enabled = false;
                textBoxPassword.Text = "Cargando...";
                LoadServerData(_serverName);
            }
        }

        private void LoadServerData(string serverName)
        {
            Servers serverData = GetServerDataByName(serverName);

            if (serverData != null)
            {
                _serverId = serverData.Id;
                textBoxName.Text = serverData.Name;
                textBoxName.Enabled = true;
                textBoxIp.Text = serverData.Ip;
                textBoxIp.Enabled = true;
                textBoxUser.Text = serverData.User;
                textBoxUser.Enabled = true;
                textBoxPassword.Text = serverData.Password;
                textBoxPassword.Enabled = true;
            }
        }

        private Servers GetServerDataByName(string serverName)
        {
            List<Servers> serverList = LoadServersFromXml();
            return serverList.Find(s => s.Name == serverName);
        }

        private List<Servers> LoadServersFromXml()
        {
            string xmlFilePath = "Servers.xml";

            if (File.Exists(xmlFilePath))
            {
                using (FileStream fs = new FileStream(xmlFilePath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Servers>));
                    return (List<Servers>)serializer.Deserialize(fs);
                }
            }
            return new List<Servers>();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonAccept_Click(object sender, EventArgs e)
        {
            try
            {
                string name = textBoxName.Text.Trim();
                string ip = textBoxIp.Text.Trim();
                string user = textBoxUser.Text.Trim();
                string password = textBoxPassword.Text.Trim();

                try
                {

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                    {
                        MessageBox.Show("Ha ocurrido un error agregando el servidor, por favor complete todos los campos", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        using (var client = new SshClient(ip, user, password))
                        {
                            client.Connect();

                            Servers serverData = new Servers
                            {
                                Name = name,
                                Ip = ip,
                                User = user,
                                Password = password
                            };

                            if (_serverId > 0)
                            {
                                try
                                {
                                    bool success = ModifyServerData(_serverId, serverData);
                                    if (success)
                                    {
                                        MessageBox.Show("Se ha modificado la información del servidor", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        ServerAdded?.Invoke(this, EventArgs.Empty);
                                        DialogResult = DialogResult.OK;
                                        this.Close();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Ha ocurrido un error modificando el servidor", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Ha ocurrido un error modificando el servidor " + $"Error: {ex.Message}");
                                }
                            }
                            else
                            {
                                try
                                {
                                    bool success = AddServerData(serverData);
                                    if (success)
                                    {
                                        MessageBox.Show("Se ha agregado un nuevo servidor", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        ServerAdded?.Invoke(this, EventArgs.Empty);
                                        DialogResult = DialogResult.OK;
                                        this.Close();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Ha ocurrido un error agregando el servidor", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Ha ocurrido un error agregando el servidor " + $"Error: {ex.Message}");
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_serverId > 0)
                    {
                        MessageBox.Show("Ha ocurrido un error modificando el servidor " + $"Error: {ex.Message}");
                    }
                    else
                    {
                        MessageBox.Show("Ha ocurrido un error agregando el servidor " + $"Error: {ex.Message}");
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }
        }


        private bool ModifyServerData(int serverId, Servers updatedServerData)
        {
            List<Servers> serverList = LoadServersFromXml();

            Servers existingServer = serverList.Find(s => s.Id == serverId);

            if (existingServer != null)
            {
                existingServer.Name = updatedServerData.Name;
                existingServer.Ip = updatedServerData.Ip;
                existingServer.User = updatedServerData.User;
                existingServer.Password = updatedServerData.Password;

                SaveServersToXml(serverList);

                return true;
            }

            return false;
        }

        private bool AddServerData(Servers newServerData)
        {
            List<Servers> serverList = LoadServersFromXml();

            int newId = GenerateUniqueId(serverList);

            newServerData.Id = newId;

            serverList.Add(newServerData);

            SaveServersToXml(serverList);

            return true;
        }

        private void SaveServersToXml(List<Servers> serverList)
        {
            string xmlFilePath = "Servers.xml";

            using (FileStream fs = new FileStream(xmlFilePath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Servers>));
                serializer.Serialize(fs, serverList);
            }
        }

        private int GenerateUniqueId(List<Servers> serverList)
        {
            int maxId = 0;

            foreach (var server in serverList)
            {
                if (server.Id > maxId)
                {
                    maxId = server.Id;
                }
            }

            return maxId + 1;
        }

        private void CRUDServerView_Load(object sender, EventArgs e)
        {

        }
    }
}
