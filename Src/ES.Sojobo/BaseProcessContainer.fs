﻿namespace ES.Sojobo

open System
open System.Collections.Generic
open ES.Sojobo.Model
open B2R2
open B2R2.FrontEnd
open B2R2.BinFile

[<AbstractClass>]
type BaseProcessContainer(pointerSize: Int32) =
    let mutable _activeRegion: MemoryRegion option = None
    let _stepEvent = new Event<IProcessContainer>()   
    let _symbols = new Dictionary<UInt64, Symbol>()
    let _pid = Guid.NewGuid().GetHashCode() |> uint32

    member val PointerSize = pointerSize with get

    abstract ProgramCounter: EmulatedValue with get
    abstract GetImportedFunctions: unit -> Symbol seq
    abstract GetInstruction: unit -> Instruction    
    abstract GetCallStack: unit -> UInt64 array
    abstract Memory: MemoryManager with get
    abstract Cpu: Cpu with get

    member internal this.UpdateActiveMemoryRegion(memRegion: MemoryRegion) =
        _activeRegion <- Some memRegion

    member this.GetActiveMemoryRegion() =
        _activeRegion.Value

    member this.GetPointerSize() =
        pointerSize

    member this.ReadNextInstruction() =      
        _stepEvent.Trigger(this)
        let instruction = this.GetInstruction()
        let programCounter = this.ProgramCounter
        this.Cpu.SetVariable(
            {programCounter with
                Value = BitVector.add programCounter.Value (BitVector.ofUInt32 instruction.Length 32<rt>)
            })
        instruction

    member this.Step = _stepEvent.Publish 
    member this.Pid = _pid    

    member this.TryGetSymbol(address: UInt64) =
        match _symbols.TryGetValue(address) with
        | (true, symbol) -> Some symbol
        | _ -> None

    member this.SetSymbol(symbol: Symbol) =
        _symbols.[symbol.Address] <- symbol
    
    interface IProcessContainer with
        member this.ProgramCounter
            with get() = this.ProgramCounter

        member this.GetPointerSize() =
            this.GetPointerSize()

        member this.GetImportedFunctions() =
            this.GetImportedFunctions()

        member this.GetInstruction() =
            this.GetInstruction()

        member this.GetCallStack() =
            this.GetCallStack()
        
        member this.GetActiveMemoryRegion() =
            this.GetActiveMemoryRegion()

        member this.TryGetSymbol(address: UInt64) =
            this.TryGetSymbol(address)

        member this.SetSymbol(symbol: Symbol) =
            this.SetSymbol(symbol)

        [<CLIEvent>]
        member this.Step
            with get() = this.Step

        member this.Memory
            with get() = this.Memory

        member this.Cpu
            with get() = this.Cpu

        member this.Pid
            with get() = this.Pid