using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;
using System.Threading;
using System;
using RedisTestNS;

namespace RedisTestNS
{
    static public class Global
    {
        public static bool Dnet_Connection;
        public static bool Dnet_readwrite;
        public static bool Dnet_error;
        public static double server_cache_alive;
        public static int client_connection_count;

    }
}

public class RedisTest : MonoBehaviour
{
    private Redis redis;
    public RedisValue redisValue;

    private string Key = "RedisTest";
    public string RedisIP_Num = "127.0.0.1";
    public int PortNum = 6379;
    public bool GetDataLoop;
    public int ThreadDelay = 1000;

    private void Start()
    {
        redis = new Redis();
        if (redis.Init(RedisIP_Num, PortNum, delegate
        {
            while (GetDataLoop)
            {
                GetData();
                Thread.Sleep(ThreadDelay);

            }
        }))
        {
            Debug.Log("connect");
        }
        else
        {
            Debug.Log("Non_connect");
        }


    }

    private void OnApplicationQuit()
    {
        GetDataLoop = false;
        //redis.T.Join();
    }

    public void GetData()
    {
        Global.Dnet_Connection = (redis.GetHash_Bool(Key, "RedisTest-connection"));
        Global.Dnet_readwrite = (redis.GetHash_Bool(Key, "RedisTest-readwrite"));
        Global.Dnet_error = (redis.GetHash_Bool(Key, "device-net-error"));
        Global.server_cache_alive = (redis.GetHash_Double(Key, "server-cache-alive"));
        Global.client_connection_count = (redis.GetHash_Int(Key, "client-connection-count"));

    }

}

public class Redis
{
    private ConnectionMultiplexer redisConnection;
    private IDatabase DB;
    private ISubscriber sub;
    public delegate void Events();
    public Thread thread;
    public bool Init(string host, int port)
    {

        this.redisConnection = ConnectionMultiplexer.Connect(host + ":" + port);
        if (redisConnection.IsConnected)
        {
            this.DB = this.redisConnection.GetDatabase();
            sub = redisConnection.GetSubscriber();
            return true;
        }

        return false;
    }

    public bool Init(string host, int port, Events LoopEvents)
    {
        thread = new Thread(new ThreadStart(LoopEvents));
        try
        {
            ConfigurationOptions option = new ConfigurationOptions
            {
                ConnectTimeout = 15000,
                AbortOnConnectFail = false,
                EndPoints = { $"{host}:{port}" },
            };
            redisConnection = ConnectionMultiplexer.Connect(option);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        if (redisConnection.IsConnected)
        {
            this.DB = this.redisConnection.GetDatabase();
            sub = redisConnection.GetSubscriber();
            thread.Start();

            return true;
        }

        return false;
    }

    public string GetString(string key)
    {
        return this.DB.StringGet(key);
    }

    public bool SetString(string key, string val)
    {
        return this.DB.StringSet(key, val);
    }

    public RedisValue GetHash(string key, string val)
    {
        return this.DB.HashGet(key, val);
    }

    public bool GetHash_Bool(string key, string val)
    {
        var data = this.DB.HashGet(key, val);
        if (data.ToString()[0] == 't' || data.ToString()[0] == 'T')
        {
            return true;
        }
        return false;
    }

    public int GetHash_Int(string key, string val)
    {
        var data = this.DB.HashGet(key, val);
        int Idata = 0;
        if (data.HasValue == true)
        {
            int DataSize = data.ToString().Length - 1;
            int sizenum = (int)Math.Pow(10, DataSize);
            foreach (var d in data.ToString())
            {
                if ('0' <= d && d <= '9')
                {
                    Idata += ((d - 48) * sizenum);
                    sizenum /= 10;
                }
                else
                {
                    return -int.MaxValue;
                }
            }
        }
        return Idata;
    }

    public double GetHash_Double(string key, string val)
    {
        var data = this.DB.HashGet(key, val);
        if (data.HasValue == true)
        {
            double Idata = Double.Parse(data);
            return Idata;
        }
        return -Double.MaxValue;
    }

    public void SetHash(string key, HashEntry[] val)
    {
        this.DB.HashSet(key, val);
    }

    public void DestroyHash(string key, RedisValue field)
    {
        this.DB.HashDelete(key, field);
    }

    public void Meseage(string Channel, string Meseage)
    {
        sub.Publish(Channel, Meseage);
    }

}