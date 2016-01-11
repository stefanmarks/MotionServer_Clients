package mocap;


import java.text.MessageFormat;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.LogRecord;

/**
 * Log handler for compact output.
 * 
 * @author  Stefan Marks
 * @version 1.0 - 24.06.2014: Created
 */
class CompactLogHandler extends Handler 
{
    public CompactLogHandler()
    {
        message = new StringBuilder();
        timestampOffset = System.currentTimeMillis();
    }

    
    @Override
    public void publish(LogRecord record)
    {
        if (isLoggable(record))
        {
            String msg = format(record);
            synchronized(System.out)
            {
                if (record.getLevel().intValue() <= Level.INFO.intValue())
                {
                    System.out.println(msg);
                    System.out.flush();
                } 
                else
                {
                    System.err.println(msg);
                    System.err.flush();
                }
            }
        }
    }

    
    @Override
    public void flush()
    {
//        System.out.flush();
//        System.err.flush();
    }

    
    @Override
    public void close() throws SecurityException
    {
    }
    

    private String format(LogRecord record)
    {
        long timeMs = record.getMillis() - timestampOffset;
        long ms   = timeMs % 1000;
        long sec  = (timeMs / 1000) % 60;
        long min  = (timeMs / 1000 / 60) % 60;
        long hour = timeMs / 1000 / 60 / 60;
        message.delete(0, message.length());
        message.append(record.getLevel().getName().charAt(0))
                .append(String.format(" %02d:%02d:%02d.%03d\t", hour, min, sec, ms))
                .append(record.getLoggerName())
                .append(": ")
                .append(MessageFormat.format(
                        record.getMessage(), 
                        record.getParameters()));
        return message.toString();
    }
    
    
    private final StringBuilder message;
    private final long timestampOffset;
}
