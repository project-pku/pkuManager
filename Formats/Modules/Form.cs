﻿using pkuManager.Alerts;
using pkuManager.Formats.Fields;
using pkuManager.Formats.pku;
using pkuManager.Utilities;
using System.Collections.Generic;
using static pkuManager.Formats.PorterDirective;

namespace pkuManager.Formats.Modules;

public interface Form_O
{
    public IntegralField Form { get; }
}

public interface Form_E
{
    public pkuObject pku { get; }
    public List<Alert> Warnings { get; }
    public string FormatName { get; }

    public Form_O Data { get; }

    public void ProcessFormBase()
        => Data.Form.SetAs(DexUtil.GetSpeciesIndexedValue<int?>(pku, FormatName, "Form Indices").Value);

    [PorterDirective(ProcessingPhase.FirstPass)]
    protected void ProcessForm() => ProcessFormBase();
}