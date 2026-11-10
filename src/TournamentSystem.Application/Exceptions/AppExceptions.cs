using System;
using System.Collections.Generic;
using System.Text;

namespace TournamentSystem.Application.Exceptions
{
    public class NotFoundException(string message) : Exception(message);

    public class ValidationException(string message) : Exception(message);

    public class ForbiddenException(string message) : Exception(message);

    internal class AppExceptions
    {
    }
}
