using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.IO;
using System.Linq;
using Google.Protobuf.Collections;

namespace Demon
{
    class Program
    {
        static MySqlConnection connection = DBUtils.GetDBConnection();

        static int port = 8006;

        static void Main(string[] args)
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, port);

            while (true)
            {
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    listenSocket.Bind(ipPoint);

                    listenSocket.Listen(10);

                    Console.WriteLine("Demon is starting...");
                    while (true)
                    {
                        Socket handler = listenSocket.Accept();
                        StringBuilder builder = new StringBuilder();

                        int bytes = 0;
                        byte[] data = new byte[65000];

                        do
                        {
                            bytes = handler.Receive(data);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        } while (handler.Available > 0);

                        int IDUser = int.Parse(builder.ToString());

                        string message = Search_All_Unread_Message(IDUser).Result;

                        //Цветовое офрмление серверной части, для наглядности обмена данными
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("<" + handler.RemoteEndPoint.ToString() + "> :");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(builder.ToString().Split('#')[0]);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write('#');

                        // отправляем ответ
                        data = Encoding.Unicode.GetBytes(message);
                        handler.Send(data);

                        //Цветовое офрмление серверной части, для наглядности обмена данными
                        int len = DateTime.Now.ToShortDateString().Length;

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("<Response Server> : ");
                        foreach (string value in message.Split('~'))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write(value);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write('~');
                        }
                        Console.WriteLine();

                        // закрываем сокет
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();

                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    listenSocket.Close();
                }
                finally 
                {
                    
                }
            }
        }

        //Поиск непрочитанных сообщений для уведомлений(11#)
        static async Task<string> Search_All_Unread_Message(int ID)
        {
            string message = "NONE";
            List<int> id_messages = new List<int>();

            using (MySqlConnection conn = connection)
            {
                try
                {
                    //Открываем соединение
                    await conn.OpenAsync();

                    //Строка запроса в БД
                    string sql_cmd = "SELECT server_chats.users.User_Name, server_chats.messages.Text_Message, server_chats.messages.ID_Sender, server_chats.chats.GUID, server_chats.messages.ID FROM server_chats.messages LEFT JOIN server_chats.users ON server_chats.messages.ID_Sender = server_chats.users.ID LEFT JOIN server_chats.chats ON ((server_chats.chats.ID_User_1 = ID_Sender) AND (server_chats.chats.ID_User_2 = @ID)) OR ((server_chats.chats.ID_User_1 = @ID) AND (server_chats.chats.ID_User_2 = ID_Sender)) WHERE (server_chats.messages.ID_Reciever = @ID AND server_chats.messages.Visible_Message = 0 AND server_chats.messages.visible_notification = 0);";

                    //Создаем команду для запроса
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql_cmd;

                    //Добавляем параметры
                    MySqlParameter id = new MySqlParameter("@ID", MySqlDbType.Int32);
                    id.Value = ID;
                    cmd.Parameters.Add(id);

                    //Начинаем считывать данные
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            message = "";
                            while (reader.Read())
                            {
                                message += reader.GetString(0) + "~" + reader.GetString(1) + "~" + reader.GetString(3) + "%";
                                id_messages.Add(int.Parse(reader.GetString(4)));
                            }
                            message = message.Substring(0, message.Length - 1);
                        }
                    }

                    //Отмечаем непрочитанные сообщения, чтобы не повторялись в оповещении 
                    foreach (int i in id_messages)
                    {
                        cmd.Parameters.Clear();
                        //Запрос на обновление
                        sql_cmd = "UPDATE server_chats.messages SET server_chats.messages.visible_notification = 1 WHERE ID = @IDMESSAGES;";
                        cmd.CommandText = sql_cmd;
                        MySqlParameter id_msg = new MySqlParameter("IDMESSAGES", MySqlDbType.Int32);
                        id_msg.Value = i;
                        cmd.Parameters.Add(id_msg);
                        cmd.ExecuteNonQuery();
                    }
                    id_messages.Clear();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    //Закрываем соединение
                    await conn.CloseAsync();
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return message;
        }
    }
}