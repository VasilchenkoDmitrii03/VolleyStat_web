﻿using ActionsLib;
using ActionsLib.ActionTypes;
using ActionsLib.TextRepresentation;
namespace WebApplication1.Models
{
    public class TimedData
    {
        
        public string Description { get; set; }  // Строковое описание
        public double TimeCode { get; set; }     // Таймкод, например, время в секундах или минутах
        public TimedData(PlayerAction p, ActionsMetricTypes AMT)
        {
            PlayerActionTextRepresentation textRepr = new PlayerActionTextRepresentation(p.VolleyActionType, AMT[p.VolleyActionType]);
            textRepr.SetPlayer(p.Player);
            foreach(MetricType m in p.MetricTypes)
            {
                textRepr.SetMetricByObject(m, p[m].Value);
            }
            this.Description = textRepr.LongStringFormat();
            this.TimeCode = p.TimeCode;
        }
    }
}
