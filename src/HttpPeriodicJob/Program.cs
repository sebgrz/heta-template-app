﻿using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using HttpPeriodicJob.Jobs;

var cron = Environment.GetEnvironmentVariable("KUBERNETES_MODE") ?? "0 0/1 * * * ?";
var isKuberenetesMode = bool.Parse(Environment.GetEnvironmentVariable("KUBERNETES_MODE") ?? "false");

if (isKuberenetesMode)
{
    // https://kubernetes.io/docs/concepts/workloads/controllers/cron-jobs/
    Console.WriteLine("k8s mode");
    await (new UpdateHttpTriggerJob()).Execute(null);
}
else
{
    Console.WriteLine("cron mode");
    await CronScheduler();
}

async Task CronScheduler()
{
    var isRunning = true;
    StdSchedulerFactory factory = new();
    var scheduler = await factory.GetScheduler();

    await scheduler.Start();

    var job = JobBuilder.Create<UpdateHttpTriggerJob>().Build();
    var trigger = TriggerBuilder.Create().WithCronSchedule(cron).StartNow().Build();

    await scheduler.ScheduleJob(job, trigger);

    Console.CancelKeyPress += (s, e) =>
    {
        isRunning = false;
        e.Cancel = true;
    };

    while (isRunning)
    {
        await Task.Delay(100);
    }
    await scheduler.Shutdown();
    Console.WriteLine("service stopped");
}