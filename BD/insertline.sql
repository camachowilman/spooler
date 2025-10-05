CREATE OR REPLACE FUNCTION public.insertline(v_document_reference character varying, v_printer_serial character varying, t_document_json text, v_document_type character varying, b_reprint boolean)
 RETURNS boolean
 LANGUAGE plpgsql
AS $function$
 
BEGIN

	PERFORM set_config('timezone', 'America/New_York', true);

	IF NOT EXISTS (
				   SELECT * FROM PUBLIC."line"
				   WHERE upper("document_reference")=upper(v_document_reference)
				   and upper("printer_serial")=upper(v_printer_serial)
				   and upper("document_type")=upper(v_document_type)
				   ) THEN
		INSERT INTO PUBLIC."line" ("document_reference","printer_serial","document_json","document_type","created_date","reprint")
		VALUES (v_document_reference,v_printer_serial,t_document_json,v_document_type,CURRENT_TIMESTAMP,b_reprint);
	ELSE
		UPDATE PUBLIC."line"
			SET "printer_serial"=v_printer_serial,"document_json"=t_document_json,"document_type"=v_document_type,"created_date"=CURRENT_TIMESTAMP
		WHERE upper("document_reference")=upper(v_document_reference)
		and upper("printer_serial")=upper(v_printer_serial)
		and upper("document_type")=upper(v_document_type)
		and "printed"= false ;
		
		IF upper(v_document_type)='P' then
			UPDATE PUBLIC."line"
			SET "printer_serial"=v_printer_serial,"document_json"=t_document_json,"document_type"=v_document_type,"created_date"=CURRENT_TIMESTAMP,"printed"=false 
			WHERE upper("document_reference")=upper(v_document_reference)
			and upper("printer_serial")=upper(v_printer_serial)
			and upper("document_type")=upper(v_document_type);	
		END IF;
		
	END IF;
	
	RETURN true;
END;
$function$
;

