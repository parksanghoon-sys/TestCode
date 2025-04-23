using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeSafePipeline.lib.Respones;

namespace TypeSafePipelint.Console.Test.Domain.Adapt
{

    // 명령 어댑터 인터페이스
    public interface ICommandAdapter<TCommandFrom, TCommandTo, TResponseFrom, TResponseTo>
    {
        TCommandTo Adapt(TCommandFrom command);
        IResponse<TResponseTo> Adapt(IResponse<TResponseFrom> response);
    }
}
