﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 双缓存任务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DoubleBufferTask<T>
    {
        /// <summary>
        /// 创建双缓存任务，执行任务的方法放在 <paramref name="doTask"/> 方法
        /// </summary>
        /// <param name="doTask">
        /// 执行任务的方法
        /// <para></para>
        /// 传入的 List&lt;T&gt; 就是需要执行的任务，请不要将传入的 List&lt;T&gt; 保存到本地字段
        /// </param>
        public DoubleBufferTask(Func<List<T>, Task> doTask)
        {
            _doTask = doTask;
        }

        /// <summary>
        /// 加入任务
        /// </summary>
        /// <param name="t"></param>
        public void AddTask(T t)
        {
            DoubleBuffer.Add(t);

            DoInner();
        }

        private async void DoInner()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isDoing) return;

            lock (DoubleBuffer)
            {
                if (_isDoing) return;
                _isDoing = true;
            }

            await DoubleBuffer.DoAllAsync(_doTask);

            lock (DoubleBuffer)
            {
                _isDoing = false;
                Finished?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public void Finish()
        {
            lock (DoubleBuffer)
            {
                if (!_isDoing)
                {
                    FinishTask.SetResult(true);
                    return;
                }

                Finished += (sender, args) => FinishTask.SetResult(true);
            }
        }

        /// <summary>
        /// 等待完成任务，只有在调用 <see cref="Finish"/> 之后，所有任务执行完成才能完成
        /// </summary>
        /// <returns></returns>
        public Task WaitAllTaskFinish()
        {
            return FinishTask.Task;
        }

        private TaskCompletionSource<bool> FinishTask { get; } = new TaskCompletionSource<bool>();

        private bool _isDoing;

        private event EventHandler? Finished;

        private readonly Func<List<T>, Task> _doTask;

        private DoubleBuffer<T> DoubleBuffer { get; } = new DoubleBuffer<T>();
    }
}